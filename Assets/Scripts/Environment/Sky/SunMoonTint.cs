using UnityEngine;

/**
 * Changes the sun/moon's colour throughout the day night cycle and it's glow ring. 
*/

[RequireComponent(typeof(SpriteRenderer))]
public class SunMoonTint : MonoBehaviour
{
    [SerializeField] private SunMoonPreset preset;
    [SerializeField] private SpriteRenderer disc;
    [SerializeField] private SpriteRenderer glow;
    [SerializeField] private CelestialBody body;

    private void LateUpdate()
    {
        float t = GameTime.Now;

        Gradient discPreset = (body == CelestialBody.Sun) ? preset.sunColour : preset.moonColour;
        Gradient glowPreset = (body == CelestialBody.Sun) ? preset.sunGlowColour : preset.moonGlowColour;

        disc.color = discPreset.Evaluate(t);
        glow.color = glowPreset.Evaluate(t);
    }
}
