using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Sky/Sky Preset")]
public class SkyPreset : ScriptableObject
{
    [Header("Sky")]
    public Gradient horizonColour; // five-point gradient representing the phase changes for the horizon
    public Gradient zenithColour; // five-point gradient representing the phase changes for the zenith (main sky above the horizon)
    public Gradient starAlphaCurve; // alpha value curve for phasing starfield in and out during day/night

    [Header("Global Illumination")]
    public Gradient lightColour; // gradient representing the phase changes for illuminated objects
    public Gradient hazeColour; // gradient for haze which is a horizon-adjacent colour for fog/cloud/depth layers
    public AnimationCurve intensity; // representing the light level throughout the day-night cycle
    public AnimationCurve playerLightIntensity; // representing the light level around the player specifically (higher at night, zero in the day)

    [Header("Clouds")]
    public Gradient highCloudColour;
    public Gradient midCloudColour;

    public Gradient GetTint(CloudBandLevel forLevel)
    {
        switch (forLevel)
        {
            case CloudBandLevel.High: return highCloudColour;
            case CloudBandLevel.Mid: return midCloudColour;
        }

        return new Gradient();
    }
}
