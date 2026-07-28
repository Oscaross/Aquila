using UnityEngine;

/**
 * Responsible for tinting layers which run independently of the world. This includes larger motion bodies such as fog/clouds and large structures in the backdrop of the game that respond differently to light.
*/

[RequireComponent(typeof(SpriteRenderer))]
public class LayerTint : MonoBehaviour
{
    [SerializeField] private TimeOfDay time;
    [SerializeField] private SpriteRenderer target;
    [Tooltip("The ScriptableObject preset for the colour curve in this sunrise-sunset cycle.")]
    [SerializeField] private SkyPreset preset;
    [Tooltip("The depth that the layer being tinted sits at. A higher depth biases toward the haze/horizon, while a lower (closer) depth factor biases towards the global light colour."), Range(0f, 1f)]
    [SerializeField] private float depthFactor;

    private void Reset()
    {
        target = GetComponent<SpriteRenderer>(); 
    }

    private void LateUpdate()
    {
        // Use the current haze colour and light colour to interpolate the body between the two depending on their depth factor.
        float t = time.TimeNow;

        Color haze = preset.hazeColour.Evaluate(t);
        Color lightNow = preset.lightColour.Evaluate(t);

        target.color = Color.Lerp(lightNow, haze, depthFactor);
    }
}

