using UnityEngine;

/**
 * Controls the appearance of the star field, mainly tuning the alpha throughout the day-night cycle to make them disappear in the day and reappear at night.
*/
public class StarField : MonoBehaviour
{
    [SerializeField] private SkyPreset preset;
    [SerializeField] private TimeOfDay time;
    [Tooltip("Determines how quickly the star modulates in brightness (twinkles). 0 for no twinkle.")]
    [SerializeField] private float twinkleSpeed = 0f;
    [Tooltip("Determines how significantly the star modulates in brightness (twinkles). 0 for no twinkle.")]
    [SerializeField, Range(0f, 0.5f)] private float twinkleDepth = 0f;
    [SerializeField] private float phase;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();    
    }

    void LateUpdate()
    {
        Color c = preset.starAlphaCurve.Evaluate(time.TimeNow);

        if (twinkleDepth > 0f)
            c.a *= 1f - twinkleDepth * 0.5f *
                   (1f + Mathf.Sin(Time.time * twinkleSpeed + phase)); // use the sine curve to move in and out of twinkling according to the depth and speed required

        spriteRenderer.color = c;
    }
}
