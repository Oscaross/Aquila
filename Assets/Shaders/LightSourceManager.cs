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
    
    private void LateUpdate()
    {
        int count = Mathf.Min(LightSource.Active.Count, MaxLights);

        for (int i = 0; i < count; i++)
        {
            LightSource src = LightSource.Active[i];
            Vector3 pos = src.transform.position;
            _lightData[i] = new Vector4(pos.x, pos.y, Mathf.Max(src.radius, 0.001f), src.intensity);
            _lightMeta[i]  = new Vector4((float)src.temperature, 0f, 0f, 0f);
        }

        Shader.SetGlobalVectorArray(GlobalLightDataID, _lightData);
        Shader.SetGlobalVectorArray(GlobalLightMetaID, _lightMeta);
        Shader.SetGlobalInt(GlobalLightCountID, count);
    }
}
