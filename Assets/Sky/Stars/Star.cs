using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Star : MonoBehaviour
{
    public StarType type;
    [Tooltip("The minimum distance at which any other star is allowed to spawn from this star.")]
    public float minSeparationWorldUnits;
    [Tooltip("Frame 0 is the resting frame. Frames play 0 → last → 0.")]
    [SerializeField] private Sprite[] frames;

    private SpriteRenderer sr;
    [SerializeField] private float rate, offset, animTime, cycleTime;
    [SerializeField] private int currentFrame = -1;
    private float dayNightCycleOffset; // a random variance that is added to the time when stars activate and deactivate. Stars can appear/disappear earlier or later depending on this value.

    private int LastFrame => frames.Length - 1;
    private SkyController sky;

    public void Init(float framesPerSecond, float holdTime)
    {
        sr = GetComponent<SpriteRenderer>();
        sky = SkyController.Instance;
        rate = LastFrame > 0 ? framesPerSecond : 0f;

        if (rate > 0f)
        {
            animTime = 2 * LastFrame / rate;
            cycleTime = animTime + holdTime;
            offset = Random.Range(0f, cycleTime);
        }

        dayNightCycleOffset = Random.Range(-0.05f, 0.05f);

        SetFrame(0);
    }

    public void UpdateStar(float elapsedSeconds)
    {
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, sky.GetCurrentLightSourceLevel(GameClock.NowOffsetBy(dayNightCycleOffset)));
        if (rate <= 0f) return; // static (no twinkle) so our job here is done

        float local = Mathf.Repeat(elapsedSeconds + offset, cycleTime); // how many seconds this star is into its animation loop
        SetFrame(local >= animTime ? 0 : PingPong(Mathf.FloorToInt(local * rate), LastFrame)); 
    }

    private void SetFrame(int frame)
    {
        if (frame == currentFrame) return;
        currentFrame = frame;
        sr.sprite = frames[frame];
    }

    // Small helper that gives us the sequence of frames that goes from 0 .. n then back from n .. 0 infinitely.
    private static int PingPong(int k, int n)
    {
        int period = 2 * n;
        int m = ((k % period) + period) % period;
        return n - Mathf.Abs(m - n);
    }
}