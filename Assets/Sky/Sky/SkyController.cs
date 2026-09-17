using UnityEngine;
using Random = UnityEngine.Random;

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
    
    [Header("Runtime - FOR INSPECTION ONLY")]
    [SerializeField] private Color zenith;
    [SerializeField] private Color horizon;
    [SerializeField] private Color light;
    [SerializeField] private Color haze;
    [SerializeField] private LightTemperature globalLightTemperature;
    
    public Color Zenith { get => zenith; private set => zenith = value; }
    public Color Horizon { get => horizon; private set => horizon = value; }
    public Color Light { get => light; private set => light = value; }
    public Color Haze { get => haze; private set => haze = value; }

    public float GetCurrentLightSourceLevel(float t) => illumination.lightSourceIntensity.Evaluate(t);

    static readonly int HazeColorID = Shader.PropertyToID("_GlobalHazeColor");
    private static readonly int HorizonYID = Shader.PropertyToID("_GlobalHorizonY");
    private static readonly int HorizonColorID = Shader.PropertyToID("_GlobalHorizonColor");
    private static readonly int ZenithColorID = Shader.PropertyToID("_GlobalZenithColor");
    private static readonly int LightColorID = Shader.PropertyToID("_GlobalLightColor");
    private static readonly int LightIntensityID = Shader.PropertyToID("_GlobalLightIntensity");
    private static readonly int RampExponentID = Shader.PropertyToID("_GlobalRampExponent");
    // This allows shaders like lit shaders to figure out, globally, where they should land on the ramp. Darkness peaks at midnight, and is a minimum at midday.
    private static readonly int DarknessID = Shader.PropertyToID("_GlobalDarkness");
    private static readonly int GlobalLightTemperatureID = Shader.PropertyToID("_GlobalLightTemperatureBlock");
    
    [SerializeField] private HorizonPreset currentSunrise;
    [SerializeField] private HorizonPreset currentSunset;
    [SerializeField] private float currentRampExponent;

    private void OnEnable()
    {
        if (illumination == null || currentSunrise == null || currentSunset == null)
        {
            Debug.LogError($"{name}: SkyController missing references: sky will not update properly.", this);
            enabled = false;
        }
        
        ConfigureNewHorizonPreset();
        Instance = this;
        GameClock.OnNoon += ConfigureNewHorizonPreset;
    }

    private void OnDisable()
    {
        GameClock.OnNoon -= ConfigureNewHorizonPreset;
    }

    private void LateUpdate()
    {
        float t = GameClock.Now;

        UpdateIllumination(t);
        UpdateHorizon(t);
        PublishGlobals(t);
    }
    
    /// <summary>
    /// Advances the global illumination and zenith sky colour forwards based on the current illumination profile.
    /// </summary>
    /// <param name="t">The time.</param>
    private void UpdateIllumination(float t)
    {
        zenith = illumination.zenithColour.Evaluate(t);
        light  = illumination.lightColour.Evaluate(t);
    }

    /// <summary>
    /// Sends required variables to shaders so that they can react to the day/night cycle changing.
    /// </summary>
    /// <param name="t">The time.</param>
    private void PublishGlobals(float t)
    {
        float intensity = illumination.intensity.Evaluate(t);
        float darkness = 1f - intensity;
        
        Shader.SetGlobalFloat(DarknessID, darkness);
        Shader.SetGlobalFloat(LightIntensityID, intensity);
        Shader.SetGlobalFloat(GlobalLightTemperatureID, (int) globalLightTemperature);
        Shader.SetGlobalColor(HazeColorID, Haze);
        Shader.SetGlobalFloat(HorizonYID, horizonY);
        Shader.SetGlobalColor(HorizonColorID, Horizon);
        Shader.SetGlobalColor(ZenithColorID, Zenith);
        Shader.SetGlobalColor(LightColorID, Light);
        Shader.SetGlobalFloat(RampExponentID, currentRampExponent);
    }

    /// <summary>
    /// Based on the time, determines how strong the horizon should be (strongest in the middle of a sunrise/sunset) and derives the haze from this horizon value.
    /// </summary>
    /// <param name="t">The time.</param>
    private void UpdateHorizon(float t)
    {
        float horizonStrength = 0f;
        float hazeBias;
        globalLightTemperature = LightTemperature.NEUTRAL; // the block is neutral colour unless we enter the sunrise/sunset progress code
        
        if (GameClock.TryGetSunriseProgress(t, out float p1))
        {
            globalLightTemperature = LightTemperature.MILDWARM;
            Color c = currentSunrise.horizonColour.Evaluate(p1);
            horizonStrength = Mathf.SmoothStep(0f, 1f, Mathf.Sin(p1 * Mathf.PI));
            horizon = new Color(c.r, c.g, c.b, ShaderController.QuantiseAlpha(horizonStrength));
            hazeBias = currentSunrise.hazeHorizonBias;
        }
        else if (GameClock.TryGetSunsetProgress(t, out float p2))
        {
            globalLightTemperature = LightTemperature.MILDWARM;
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
        
        // The colour of the haze, a linear interpolation between the top and bottom of the sky (zenith, horizon). The higher the horizon strength & haze bias the closer this is to the horizon colour.
        Color hazeRgb = Color.Lerp(Zenith, Horizon, ShaderController.QuantiseAlpha(horizonStrength * hazeBias));
        haze = new Color(hazeRgb.r, hazeRgb.g, hazeRgb.b, ShaderController.QuantiseAlpha(illumination.hazeStrength.Evaluate(t)));
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