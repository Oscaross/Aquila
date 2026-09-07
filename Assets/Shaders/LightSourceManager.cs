using UnityEngine;

[ExecuteAlways]
public class LightSourceManager : MonoBehaviour
{
    private const int MaxLights = 32;
    /// <summary>
    /// x, y = world pos, z = radius, w = intensity 
    /// </summary>
    private readonly Vector4[] _lightData = new Vector4[MaxLights];
    /// <summary>
    /// x = temperature (warm/cool light)
    /// y, z, w are all unused right now, they can be used in the future for stuff like flicker or other properties about the light source
    /// We give a Vector4 because the GPU won't accept something that's not a 4-dimensional array. We might as well use the space that we have to use here anyway.
    /// </summary>
    private readonly Vector4[] _lightMeta = new Vector4[MaxLights];
    
    private static readonly int GlobalLightDataID = Shader.PropertyToID("_LightData");
    private static readonly int GlobalLightMetaID = Shader.PropertyToID("_LightMeta");
    private static readonly int GlobalLightCountID = Shader.PropertyToID("_LightCount");

    private float time; // we need this to apply flicker

    private void LateUpdate()
    {
        time = (Application.isPlaying) ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
        int count = Mathf.Min(LightSource.Active.Count, MaxLights);

        for (int i = 0; i < count; i++)
        {
            LightSource src = LightSource.Active[i];
            Vector3 pos = src.transform.position;
            _lightData[i] = new Vector4(pos.x, pos.y, Mathf.Max(src.radius, 0.001f), GetIntensity(src));
            _lightMeta[i]  = new Vector4((float)src.temperature, 0f, 0f, 0f);
        }

        Shader.SetGlobalVectorArray(GlobalLightDataID, _lightData);
        Shader.SetGlobalVectorArray(GlobalLightMetaID, _lightMeta);
        Shader.SetGlobalInt(GlobalLightCountID, count);
    }

    /// <summary>
    /// Applies movement and flicker to light sources rather than them just being static sources.
    /// </summary>
    /// <param name="src">The light source we are applying flicker to.</param>
    /// <returns>Light intensity at this timestep, as a float.</returns>
    private float GetIntensity(LightSource src)
    {
        float currentIntensity = src.maxIntensity * SkyController.Instance.GetCurrentLightSourceLevel(GameTime.Now);
        if (src.flickerIntensity <= 0f || src.flickerSpeed <= 0f) return currentIntensity;

        // sin maps to 0..1 so the light oscillates between (1 - flickerIntensity) and 1.
        float wave = (Mathf.Sin(time * src.flickerSpeed * Mathf.PI * 2f + src.flickerPhase) + 1f) * 0.5f;
        wave = Mathf.Round(wave * 3f) / 3f;
        
        return currentIntensity * (1f - src.flickerIntensity + src.flickerIntensity * wave);
    }
}
