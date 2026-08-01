using UnityEngine;

public class AnimateSunMoon : MonoBehaviour
{
    [SerializeField] private float radiusX = 20f; // half the horizontal span
    [SerializeField] private float arcHeight = 10f; // peak height above horizon
    [SerializeField] private float horizonY = -3f; // ground line, in camera-local units
    [SerializeField] private float depth = 10f; // allows us to see the sun through the camera
    [SerializeField] private CelestialBody body;

    void LateUpdate()
    {
        float timeToSubtract = (body == CelestialBody.Sun) ? 0.25f : 0.75f;
        // Animates the sun/moon in its arc across the sky from dawn to dusk. Uses sin x and cos x to mimic circular movement across a parameter x (horizontal arc) and y (vertical arc).
        float angle = (GameTime.Now - timeToSubtract) * 2f * Mathf.PI;
        transform.localPosition = new Vector3(
            Mathf.Cos(angle) * -radiusX,
            horizonY + Mathf.Sin(angle) * arcHeight,
            depth);
    }
}