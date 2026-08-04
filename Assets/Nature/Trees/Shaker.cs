using UnityEngine;

/// <summary>
/// Displaces a visual on a decaying oscillation to give impact feedback.
/// Snaps to whole pixels so the shake stays crisp against pixel-art rendering.
/// </summary>
public class Shaker : MonoBehaviour
{
    [SerializeField, Tooltip("How long a single shake lasts, in seconds.")]
    private float durationSeconds = 0.25f;

    [SerializeField, Tooltip("Maximum sideways displacement, in art pixels.")]
    private float amplitudePixels = 3f;

    [SerializeField, Tooltip("Oscillations per second. Higher feels sharper.")]
    private float frequency = 30f;

    private Vector3 restPos;
    private float elapsed;
    private bool shaking;

    private void Awake() => restPos = transform.localPosition;

    /// <summary>Starts (or restarts) the shake.</summary>
    public void Shake()
    {
        elapsed = 0f;
        shaking = true;
    }

    private void LateUpdate()
    {
        if (!shaking) return;

        elapsed += Time.deltaTime;

        if (elapsed >= durationSeconds)
        {
            transform.localPosition = restPos;
            shaking = false;
            return;
        }

        float decay = 1f - (elapsed / durationSeconds);
        float offsetPixels = Mathf.Sin(elapsed * frequency) * amplitudePixels * decay;
        float offset = Mathf.Round(offsetPixels) / GlobalConstants.PixelsPerUnit;

        transform.localPosition = restPos + new Vector3(offset, 0f, 0f);
    }
}