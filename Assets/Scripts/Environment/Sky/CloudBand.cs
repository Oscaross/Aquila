using UnityEngine;

/**
 * Controls spawning, updating and maintaining of clouds in distinct cloud bands including the wind speed, how long it takes for them to respawn, their tinting during phases of the day/night cycle, etc... 
*/

public class CloudBand : MonoBehaviour
{
    [SerializeField] private TimeOfDay time;
    [SerializeField] private SkyPreset preset;
    [SerializeField] private CloudBandLevel level;
    [SerializeField] private float windSpeed = 0.7f;
    [SerializeField] private float tolerance = 2f;
    [SerializeField] private float parallaxFactor = 0f; // (if any) add parallax to clouds or ensure they remain fixed to the camera

    /**
     * Per individual cloud class:
    */

    class Cloud
    {
        public Transform transform; // pos of cloud
        public SpriteRenderer spriteRenderer; // renderer for tinting of colour/alpha
        public float speedMultiplier; // the randomised deviation of the cloud itself from the band's wind speed
        public float startX; // where the cloud initially drifted from
        public float drift; // how much the cloud has drifted
    }

    private Camera cam;
    private float startCamX;
    private Cloud[] clouds; // all clouds in this band

    void Awake()
    {
        cam = Camera.main;
        startCamX = cam.transform.position.x;

        int n = transform.childCount;
        clouds = new Cloud[n];

        // Initialise all clouds through the internal Cloud class.
        for (int i = 0; i < n; i++)
        {
            Transform t = transform.GetChild(i);
            clouds[i] = new Cloud
            {
                transform = t,
                spriteRenderer = t.GetComponent<SpriteRenderer>(),
                speedMultiplier = Random.Range(0.85f, 1.15f),
                startX = t.position.x,
                drift = 0f
            };

            if (clouds[i].spriteRenderer == null)
            {
                Debug.LogError($"Cloud '{t.name}' has no SpriteRenderer but is being passed as a cloud.");
            }
        }
    }

    void LateUpdate()
    {
        Color tint = preset.GetTint(level).Evaluate(time.TimeNow);
        float camHalf = cam.orthographicSize * cam.aspect;
        float camX = cam.transform.position.x;
        float camTravel = camX - startCamX;

        for (int i = 0; i < clouds.Length; i++)
        {
            Cloud c = clouds[i];

            Transform t = c.transform;
            SpriteRenderer r = c.spriteRenderer;

            c.drift += c.speedMultiplier * windSpeed * Time.deltaTime; // the cloud drifts by its multiplier times the band speed scaled by deltaTime
            float x = c.startX + camTravel * (1f - parallaxFactor) + c.drift; // how far we have travelled in total from the cloud's origin

            float limit = camHalf + r.bounds.extents.x + tolerance; // the maximum distance a cloud travels before wraparound
            float offset = x - camX; // the disparity between where we are now and the cloud's origin

            // Check if we need a left -> right or right -> left wrap because our offset has exceeded the limit for wrapping.
            if (offset > limit) { c.drift -= 2f * limit; x -= 2f * limit; }
            else if (offset < -limit) { c.drift += 2f * limit; x += 2f * limit; }

            // Assignment of new values
            Vector3 oldPos = t.position;
            oldPos.x = x;
            t.position = oldPos;

            r.color = tint;
        }
    }
}