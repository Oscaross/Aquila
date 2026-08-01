using UnityEngine;

/*
 * Scrolls a background layer at a fraction of the camera's movement, creating the
 * illusion of depth. Positions are snapped to the pixel grid so layers step cleanly
 * rather than shimmering against the pixel-perfect camera.
 */
public class ParallaxLayer : MonoBehaviour
{
    private const float PixelsPerUnit = 16f;

    [Range(0f, 1f)]
    [Tooltip("0 = distant (barely scrolls relative to the world), 1 = foreground (fixed in the world)")]
    public float parallaxFactor = 1f;

    private Transform cam;
    private Vector3 startPos;
    private float startCamX;

    private void Start()
    {
        cam = Camera.main.transform;
        startPos = transform.position;
        startCamX = cam.position.x;
    }

    private void LateUpdate()
    {
        float camTravel = cam.position.x - startCamX;
        float x = startPos.x + camTravel * (1f - parallaxFactor);

        // Snap the layer's offset from the camera to whole pixels, so the rendered
        // position is always a clean pixel boundary relative to what the camera shows.
        float rel = Mathf.Round((x - cam.position.x) * PixelsPerUnit) / PixelsPerUnit;
        float camSnapped = Mathf.Round(cam.position.x * PixelsPerUnit) / PixelsPerUnit;

        transform.position = new Vector3(camSnapped + rel, startPos.y, startPos.z);
    }
}