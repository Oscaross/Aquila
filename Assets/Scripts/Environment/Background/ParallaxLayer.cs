using UnityEngine;

/*
 * Controls the parallax effect while rendering backgrounds. Backgrounds scroll at various speeds, giving the player the illusion of movement over longer distances. 
*/

public class ParallaxLayer : MonoBehaviour 
{
    [Range(0f, 1f)]
    [Tooltip("0 = distant (barely scrolls), 1 = foreground (scrolls with the world)")]
    public float parallaxFactor = 1f;

    [Header("Drifting")]

    [Tooltip("0 for no movement besides when the player moves, otherwise set a drift speed for autonomous movement")]
    [SerializeField] private float driftSpeed = 0f;
    [Tooltip("Wrapping causes an object to be teleported to the opposite side of the player once it drifts out of view.")]
    [SerializeField] private bool shouldWrap = false;
    [Tooltip("The number of pixels extra that a sprite travels plus its width and the camera viewport width is the distance in world units (1WU = 16px) a sprite travels before wrapping back around.")]
    [SerializeField] private float tolerance = 10f;


    private Transform cam;
    private float drift; // how far we've deviated from our player-adjusted transform
    private Vector3 startPos;
    private float startCamX;
    private float wrapWidth = 0f;

    private void Start()
    {
        // Initially our camera and lastCamPos are the same. They are simply the position of the camera.
        cam = Camera.main.transform;
        startPos = transform.position;
        startCamX = cam.position.x;

        if (shouldWrap)
        {
            float camHalfWidth = Camera.main.orthographicSize * Camera.main.aspect; // what's half of our camera view in world units?
            float spriteHalfWidth = GetComponent<SpriteRenderer>().bounds.extents.x; // what's half of our sprite size in world units? 

            wrapWidth = camHalfWidth + spriteHalfWidth + tolerance;
        }
    }

    // Called after each Update (i.e. draw of a frame to the screen)
    void LateUpdate()
    {
        drift += driftSpeed * Time.deltaTime;

        float camTravel = cam.position.x - startCamX;
        float x = startPos.x + camTravel * (1f - parallaxFactor) + drift;

        // wrap relative to the camera, so teleports always happen off-screen
        if (shouldWrap)
        { 
            float offset = x - cam.position.x;
            if (offset > wrapWidth) { drift -= (2 * wrapWidth); x -= 2 * wrapWidth; }
            if (offset < -wrapWidth) { drift += (2 * wrapWidth); x += 2 * wrapWidth; }
        }

        transform.position = new Vector3(x, startPos.y, startPos.z);
    }
}
