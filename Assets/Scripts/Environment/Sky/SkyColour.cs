using UnityEngine;

/**
 * Calculates and applies the correct gradient for the sky depending on the time of day. 
*/

public class SkyColour : MonoBehaviour
{
    [SerializeField] private SpriteRenderer zenith;
    [SerializeField] private SpriteRenderer horizon;
    [SerializeField] private SkyPreset preset;

    void LateUpdate()
    {
        float t = GameTime.Now;
        zenith.color = preset.zenithColour.Evaluate(t);
        horizon.color = preset.horizonColour.Evaluate(t);
    }
}
