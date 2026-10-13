using System.Collections.Generic;
using UnityEngine;

public class CloudBand : MonoBehaviour
{
    [Tooltip("All the different cloud sprites of this band type we have to choose from.")]
    [SerializeField] private Sprite[] sprites;
    [Tooltip("The screen-space (0.0 ... 1.0) coordinates in y that this cloud band is allowed to occupy. 1 is the top of the screen, 0 is the bottom. Usually want in the top 50%")]
    [SerializeField] private Vector2 yBounds;

    [Tooltip("World units a cloud travels past the screen edge before it wraps.")] 
    [SerializeField] private float margin = 4f;
    
    private readonly List<Cloud> clouds = new();
    private Camera cam;
    
    public CloudType bandType;
    [SerializeField] private Material cloudMaterial;

    private void Awake() => cam = Camera.main;
    
    static float Snap(float v) => Mathf.Round(v * GlobalConstants.PixelsPerUnit) / GlobalConstants.PixelsPerUnit;
    
    // Camera view in this band's local space. Adjusts our total screen view from the camera to the band's constrained part of the screen.
    void GetLocalView(out float left, out float right, out float bottom, out float top)
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Vector3 cp = cam.transform.position, bp = transform.position;
        left = cp.x - halfW - bp.x;
        right = cp.x + halfW - bp.x;
        bottom = cp.y - halfH - bp.y;
        top = cp.y + halfH - bp.y;
    }

    public void SpawnClouds(int count)
    {
        GetLocalView(out float left, out float right, out _, out _);
        float spanL = left - margin, spanR = right + margin;
        float slot = (spanR - spanL) / count;

        for (int i = 0; i < count; i++)
        {
            var c = Cloud.Create(transform, cloudMaterial, sprites[0], "Background", bandType);
            Reroll(c);
            c.localX = spanL + slot * (i + Random.Range(0.25f, 0.75f)); // stratified jitter
            clouds.Add(c);
        }
        
        MoveClouds(0f, default); // place them immediately
    }

    public void MoveClouds(float windSpeed, Direction dir) // windSpeed in world units/sec
    {
        GetLocalView(out float left, out float right, out float bottom, out float top);
        float wrapL = left - margin, wrapR = right + margin;
        float dx = windSpeed * dir.Sign() * Time.deltaTime;

        foreach (var c in clouds)
        {
            c.localX += dx;

            // We've fully passed on the left, wrap round to the right and pick a new cloud sprite
            if (c.localX + c.xMax < wrapL)  
            {
                Reroll(c);
                c.localX = right - c.xMin + Random.Range(0f, margin);
            }
            else if (c.localX + c.xMin > wrapR) // same but on the right 
            {
                Reroll(c);
                c.localX = left - c.xMax - Random.Range(0f, margin);
            }

            float y = Mathf.Lerp(bottom, top, Mathf.Lerp(yBounds.x, yBounds.y, c.yNorm));
            c.transform.localPosition = new Vector3(Snap(c.localX), Snap(y), 0f);
        }
    }

    void Reroll(Cloud c)
    {
        c.sr.sprite = sprites[Random.Range(0, sprites.Length)];
        Bounds b = c.sr.sprite.bounds; // sprite-local, relative to pivot
        c.xMin = b.min.x;
        c.xMax = b.max.x;
        c.yNorm = Random.value;
        // randomly flip about the y-axis
        
        bool flip = Random.value > 0.5f;
        c.sr.flipX = flip;
        c.xMin = flip ? -b.max.x : b.min.x;
        c.xMax = flip ? -b.min.x : b.max.x;
    }
}
