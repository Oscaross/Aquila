using UnityEngine;
using UnityEngine.Rendering.Universal;

/**
 * A randomly seeded script to control light according to some AnimationCurve that changes a light source over the day-night cycle. Attach to any light source that varies throughout the time cycle. Optional flicker depending on some tunable parameters. 
*/

[RequireComponent(typeof(Light2D))]
public class FlickeringLight : MonoBehaviour
{
    [SerializeField] private IlluminationProfile illumination;
    [Tooltip("The multiplier layered over the top of the current intensity for the flicker event. Zero for no flickering.")]
    [SerializeField, Range(0f, 0.3f)] private float flickerMagnitude = 0.08f;
    [Tooltip("The multiplier on the number of flicker cycles. Higher = faster rate of flickering.")]
    [SerializeField, Min(0f)] private float flickerSpeed = 8f;
    [Tooltip("An optional delay between the real world time and when the light source reacts. Used to add variation in e.g. buildings (so they don't all come alive at the same instant).")]
    [SerializeField, Range(-0.05f, 0.05f)] private float timeOffset = 0f;

    private Light2D light2D;
    private float seed; // make flickers out-of-sync and non-deterministic

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        seed = Random.value * 100f;
    }

    private void LateUpdate()
    {
        // Query the brightness curve according to the current time (i.e. during the day this should be mostly zero).
        float baseIntensity = illumination.GetLightSourceIntensity(GameTime.Now + timeOffset);
        // PerlinNoise returns a smoothly varying value in 0 to 1. Randomised samples are dependent and connected, meaning we get a "random walk" of flickering according to the flicker speed, magnitude and seed.
        float flicker = 1f + flickerMagnitude * (Mathf.PerlinNoise(Time.time * flickerSpeed + seed, 0f) - 0.5f) * 2f;
        light2D.intensity = baseIntensity * flicker;
    }
}
