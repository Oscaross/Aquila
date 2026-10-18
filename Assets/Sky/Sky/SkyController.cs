using UnityEngine;
using Random = UnityEngine.Random;

public class SkyController : MonoBehaviour
{
    public static SkyController Instance { get; private set; }
    
    [Header("Main Sky")]
    
    [SerializeField] private HorizonPreset[] sunrises;
    [SerializeField] private HorizonPreset[] sunsets;
    [SerializeField] private float twilightProbability = 0.5f;
    [SerializeField] private IlluminationProfile illumination;
    [SerializeField] private float minRampExponent = 0.2f;
    [SerializeField] private float maxRampExponent = 1.5f;

    [Header("Clouds")] 
    [Tooltip("The colour preset for each component of the cloud (high, middle and low part) that gets assigned to during the day/night. The sky colour lerps between this at sunrise/sunset.")]
    [SerializeField] private CloudColours cloudColoursDay, cloudColoursNight;
    
    [Header("Runtime - FOR INSPECTION ONLY")]
    [SerializeField] private Color zenith;
    [SerializeField] private Color horizon;
    [SerializeField] private Color light;
    [SerializeField] private Color haze;
    [SerializeField] private LightTemperature globalLightTemperature;
    [Tooltip("How spectacular the sunrise/sunset is. Deterministic by the seed and day counter, set for each sunrise/sunset separately. " +
             "All atmospheric effects around sunrise/sunset consume from this value, such as the strength of sunrise/sunset, cloud colour, haze colour, etc...")]
    [SerializeField] private float currentTwilightStrength;

    [SerializeField] private bool isTwilightActive;
    [SerializeField] private Color[] cloudColours = new Color[3];
    
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
    private static readonly int TwilightStrengthID = Shader.PropertyToID("_GlobalTwilightStrength");
    private static readonly int RampExponentID = Shader.PropertyToID("_GlobalRampExponent");
    
    // This allows shaders like lit shaders to figure out, globally, where they should land on the ramp. Darkness peaks at midnight, and is a minimum at midday.
    private static readonly int DarknessID = Shader.PropertyToID("_GlobalDarkness");
    private static readonly int GlobalLightTemperatureID = Shader.PropertyToID("_GlobalLightTemperatureBlock");

    private static readonly int HiCloudID = Shader.PropertyToID("_CloudHi");
    private static readonly int MidCloudID = Shader.PropertyToID("_CloudMid");
    private static readonly int LoCloudID = Shader.PropertyToID("_CloudLo");

    private static readonly int TwilightSalt = Pseudorandom.SaltOf("daynight-twilight");
    private static readonly int TwilightStrengthSalt = Pseudorandom.SaltOf("daynight-twilight-strength");
    
    [SerializeField] private HorizonPreset currentSunrise;
    [SerializeField] private HorizonPreset currentSunset;

    private void Start() => ConfigureNewHorizonPreset();
    
    private void OnEnable()
    {
        if (illumination == null || currentSunrise == null || currentSunset == null)
        {
            Debug.LogError($"{name}: SkyController missing references: sky will not update properly.", this);
            enabled = false;
        }
        
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
        UpdateCloudColours();
        PublishGlobals(t); // comes last because it's after we've evaluated everything for this frame
    }
    
    /// <summary>
    /// Advances the global illumination and zenith sky colour forwards based on the current illumination profile.
    /// </summary>
    /// <param name="t">The time.</param>
    private void UpdateIllumination(float t)
    {
        zenith = illumination.zenithColour.Evaluate(t);
        light = illumination.lightColour.Evaluate(t);
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
        Shader.SetGlobalFloat(HorizonYID, GlobalConstants.GroundY);
        Shader.SetGlobalColor(HorizonColorID, Horizon);
        Shader.SetGlobalColor(ZenithColorID, Zenith);
        Shader.SetGlobalColor(LightColorID, Light);
        Shader.SetGlobalFloat(RampExponentID, RampExponent);
        Shader.SetGlobalFloat(TwilightStrengthID, currentTwilightStrength);
        Shader.SetGlobalColor(HiCloudID, cloudColours[0]);
        Shader.SetGlobalColor(MidCloudID, cloudColours[1]);
        Shader.SetGlobalColor(LoCloudID, cloudColours[2]);
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
            horizonStrength = GetHorizonStrength(p1);
            horizon = new Color(c.r, c.g, c.b, ShaderController.QuantiseAlpha(horizonStrength));
            hazeBias = currentSunrise.hazeHorizonBias;
        }
        else if (GameClock.TryGetSunsetProgress(t, out float p2))
        {
            globalLightTemperature = LightTemperature.MILDWARM;
            Color c = currentSunset.horizonColour.Evaluate(p2);
            horizonStrength = GetHorizonStrength(p2);
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

        currentSunrise = sunrises[Random.Range(0, sunrises.Length)];
        currentSunset = sunsets[Random.Range(0, sunsets.Length)];
        
        isTwilightActive = Pseudorandom.Hash01(GameClock.Day, TwilightSalt) > 1 - twilightProbability;
        currentTwilightStrength = RandomTwilightStrength();
    }

    // How much the horizon contributes vs. the zenith during sunrise/sunset.
    private float GetHorizonStrength(float intensity)
    {
        return isTwilightActive ? Mathf.SmoothStep(0f, 1f, Mathf.Sin(intensity * Mathf.PI)) : 0f;
    }
    

    /// <summary>
    /// Depending on the key, returns the twilight strength for this day and seed. Returns 0 if we do not exceed the twilight probability for this day.
    /// </summary>
    /// <returns></returns>
    private float RandomTwilightStrength() =>
        isTwilightActive ? Pseudorandom.HashRange(1, 11, GameClock.Day, TwilightStrengthSalt) / 10f : 0f;

    private float RampExponent => currentTwilightStrength == 0
        ? 0f
        : Mathf.Lerp(minRampExponent, maxRampExponent, currentTwilightStrength);

    /// <summary>
    /// Smoothly interpolates cloud colour between nighttime, day time, and (eventually) storm clouds.
    /// Randomly tints clouds at sunset/sunrise.
    /// </summary>
    /// <param name="t"></param>
    private void UpdateCloudColours()
    {
        float n = NightWeight();
        cloudColours[0] = Color.Lerp(cloudColoursDay.high, cloudColoursNight.high, n).linear;
        cloudColours[1] = Color.Lerp(cloudColoursDay.mid, cloudColoursNight.mid, n).linear;
        cloudColours[2] = Color.Lerp(cloudColoursDay.low, cloudColoursNight.low, n).linear;
        
    }
    
    // How much the cloud's night weight colour contributes vs the day weight. A shorthand for making it so that nighttime cloud colours fade in and out during the night.
    private static float NightWeight()
    {
        float t = GameClock.Now;
        if (GameClock.TryGetSunriseProgress(t, out float p)) return 1f - Mathf.SmoothStep(0f, 1f, p);
        if (GameClock.TryGetSunsetProgress(t, out p)) return Mathf.SmoothStep(0f, 1f, p);
        return GameClock.IsNight ? 1f : 0f;
    }
}