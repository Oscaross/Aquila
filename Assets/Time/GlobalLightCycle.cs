using UnityEngine;
using UnityEngine.Rendering.Universal;

/**
 * Controls the shifting of hues to cooler (darker)/warmer (brighter) depending on the time of day. Applies globally to all "illuminated" assets. 
*/
public class GlobalLightCycle : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private IlluminationProfile profile;

    private void LateUpdate()
    {
        float t = GameTime.Now;
        globalLight.color = profile.lightColour.Evaluate(t);
        globalLight.intensity = profile.intensity.Evaluate(t);
    }
}
