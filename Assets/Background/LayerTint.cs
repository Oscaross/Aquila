using UnityEngine;

/**
 * Responsible for tinting layers which run independently of the world. This includes larger motion
 * bodies such as fog/clouds and large structures in the backdrop that respond differently to light.
 * Haze is handled by the Aquila/Haze shader via per-material _LayerHaze.
*/
[RequireComponent(typeof(SpriteRenderer))]
public class LayerTint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private SkyController sky;
    [Tooltip("How strongly this body responds to the global light colour."), Range(0f, 1f)]
    [SerializeField] private float lightResponse = 1f;

    private void Reset() => target = GetComponent<SpriteRenderer>();

    private void LateUpdate()
    {
        if (sky == null) return;
        target.color = Color.Lerp(Color.white, sky.Light, lightResponse);
    }
}