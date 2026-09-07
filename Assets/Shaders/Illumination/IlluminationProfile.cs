using UnityEngine;

[CreateAssetMenu(fileName = "IlluminationProfile", menuName = "Scriptable Objects/Sky/IlluminationProfile")]
public class IlluminationProfile : ScriptableObject
{
    [Tooltip("The colour curve that the zenith (top part of the sky) follows. Can be modified by weather, such as making the sky go gray or white rather than blue.")]
    public Gradient zenithColour;
    [Tooltip("The global light tint that all sprites using the Lit material use.")]
    public Gradient lightColour;
    [Tooltip("The global light intensity curve throughout the day/night cycle. Light sources and torches complement this, so they are 1 - this at all times in the day.")]
    public AnimationCurve intensity;
    [Tooltip("How much atmospheric hazing is present throughout the day/night cycle.")]
    public AnimationCurve hazeStrength;
    [Tooltip("The level that light sources like torches and lanterns should follow throughout the cycle.")]
    public AnimationCurve lightSourceIntensity;
}
