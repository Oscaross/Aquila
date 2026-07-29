using UnityEngine;

/**
 * Functions similarly to SkyPreset. Retains information about the sun and moon's colour, alpha and lighting throughout the day night cycle. 
 * 
*/
[CreateAssetMenu(fileName = "SunMoonPreset", menuName = "Sky/SunMoon Preset")]
public class SunMoonPreset : ScriptableObject
{
    [Header("Sun")]
    public Gradient sunColour;
    public Gradient sunGlowColour;
    public AnimationCurve sunGlowScale = AnimationCurve.Constant(0f, 1f, 1f);

    [Header("Moon")]
    public Gradient moonColour;
    public Gradient moonGlowColour;
    public AnimationCurve moonGlowScale = AnimationCurve.Constant(0f, 1f, 1f);
}
