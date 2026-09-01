using UnityEngine;
using Random = UnityEngine.Random;

/**
 * 
*/

[ExecuteAlways]
public class SkyController : MonoBehaviour
{
    public static SkyController Instance { get; private set; }


    [SerializeField] private HorizonPreset[] sunrises;
    [SerializeField] private HorizonPreset[] sunsets;
    [SerializeField] private float minRampExponent;
    [SerializeField] private float maxRampExponent;
    
    [SerializeField] private IlluminationProfile illumination;
    [SerializeField] private float horizonY = 0f;

    [SerializeField] private ShaderController shaderController;

    [Header("Runtime (read-only)")]
    [SerializeField] private Color zenith;
    [SerializeField] private Color horizon;
    [SerializeField] private Color light;
    [SerializeField] private Color haze;
    
    public Color Zenith { get => zenith; private set => zenith = value; }
    public Color Horizon { get => horizon; private set => horizon = value; }
    public Color Light { get => light; private set => light = value; }
    public Color Haze { get => haze; private set => haze = value; }

    static readonly int HazeColorID = Shader.PropertyToID("_GlobalHazeColor");
    private static readonly int HorizonYID = Shader.PropertyToID("_GlobalHorizonY");
    private static readonly int HorizonColorID = Shader.PropertyToID("_GlobalHorizonColor");
    private static readonly int ZenithColorID = Shader.PropertyToID("_GlobalZenithColor");
    private static readonly int LightColorID = Shader.PropertyToID("_GlobalLightColor");
    private static readonly int LightIntensityID = Shader.PropertyToID("_GlobalLightIntensity");
    private static readonly int RampExponentID = Shader.PropertyToID("_GlobalRampExponent");
    private static readonly int LightOffsetID = Shader.PropertyToID("_GlobalLightOffset");
    // This allows shaders like lit shaders to figure out, globally, where they should land on the ramp. Darkness peaks at midnight, and is a minimum at midday.
    private static readonly int DarknessID = Shader.PropertyToID("_GlobalDarkness");


    [SerializeField] private HorizonPreset currentSunrise;
    [SerializeField] private HorizonPreset currentSunset;
    [SerializeField] private float currentRampExponent;


    private void OnEnable()
    {
        ConfigureNewHorizonPreset();
        Instance = this;
        TimeOfDay.OnNoon += ConfigureNewHorizonPreset;
    }

    private void OnDisable()
    {
        TimeOfDay.OnNoon -= ConfigureNewHorizonPreset;
    }

    private void LateUpdate()
    {
        if (illumination == null || currentSunrise == null || currentSunset == null) return;
        
        float t = GameTime.Now;
        // We only want the horizon to be "strong" and present in the visuals at sunrise/sunset where it's at its peak, and fade in and out from those moments.
        float horizonStrength = 0f;
        // The haze bias is how much the horizon colour contributes towards the haze. This is determined by the specific Sunset/Sunrise we've picked.
        float hazeBias;

        if (GameTime.TryGetSunriseProgress(t, out float p1))
        {
            Color c = currentSunrise.horizonColour.Evaluate(p1);
            horizonStrength = Mathf.SmoothStep(0f, 1f, Mathf.Sin(p1 * Mathf.PI));
            horizon = new Color(c.r, c.g, c.b, ShaderController.QuantiseAlpha(horizonStrength));
            hazeBias = currentSunrise.hazeHorizonBias;
        }
        else if (GameTime.TryGetSunsetProgress(t, out float p2))
        {
            Color c = currentSunset.horizonColour.Evaluate(p2);
            horizonStrength = Mathf.SmoothStep(0f, 1f, Mathf.Sin(p2 * Mathf.PI));
            horizon = new Color(c.r, c.g, c.b, ShaderController.QuantiseAlpha(horizonStrength));
            hazeBias = currentSunset.hazeHorizonBias;
        }
        else
        {
            hazeBias = 0f;
            horizon = new Color(0f, 0f, 0f, 0f);
        }
        
        zenith = illumination.zenithColour.Evaluate(t);
        light = illumination.lightColour.Evaluate(t);

        // The colour of the haze, a linear interpolation between the top and bottom of the sky (zenith, horizon). The higher the horizon strength & haze bias the closer this is to the horizon colour.
        Color hazeRgb = Color.Lerp(Zenith, Horizon, ShaderController.QuantiseAlpha(horizonStrength * hazeBias));

        haze = new Color(hazeRgb.r, hazeRgb.g, hazeRgb.b, ShaderController.QuantiseAlpha(illumination.hazeStrength.Evaluate(t)));
        
        Shader.SetGlobalColor(HazeColorID, Haze);
        Shader.SetGlobalFloat(HorizonYID, horizonY);
        Shader.SetGlobalColor(HorizonColorID, Horizon);
        Shader.SetGlobalColor(ZenithColorID, Zenith);
        Shader.SetGlobalColor(LightColorID, Light);
        Shader.SetGlobalFloat(LightIntensityID, illumination.intensity.Evaluate(t));
        Shader.SetGlobalFloat(RampExponentID, currentRampExponent);

        
        float intensity = illumination.intensity.Evaluate(t);
        float darkness = 1f - intensity;
        
        Shader.SetGlobalFloat(DarknessID, darkness);
        Shader.SetGlobalFloat(LightIntensityID, intensity);
    }

    /// <summary>
    /// Chooses a random sunrise and sunset for the day, and a random ramp exponent which is effectively the height of the horizon (controls how much horizon the player can see).
    /// </summary>
    private void ConfigureNewHorizonPreset()
    {
        Debug.Assert(sunrises.Length > 0 && sunsets.Length > 0);

        currentSunrise =  sunrises[Random.Range(0, sunrises.Length)];
        currentSunset = sunsets[Random.Range(0, sunsets.Length)];
        
        currentRampExponent = Random.Range(minRampExponent, maxRampExponent);
    }
}