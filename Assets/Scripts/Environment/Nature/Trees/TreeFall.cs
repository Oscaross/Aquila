using UnityEngine;

/**
 * Smoothly animates and handles the falling animation of a tree when it is felled. 
*/
public class TreeFall : MonoBehaviour
{
    [SerializeField] private float fallDuration = 1.2f;
    [SerializeField] private float angleStep = 7.5f;
    [SerializeField] private AnimationCurve fallCurve;

    private bool isFalling;
    private float elapsed;
    private float targetAngle;

    /// <summary>
    /// Causes a tree to cleanly fall in the given direction.
    /// </summary>
    /// <param name="direction">The direction the tree should fall. 1 for left and -1 for right.</param>
    public void Fell(int direction)
    {
        if (Mathf.Abs(direction) != 1) Debug.LogError("Tree must fall left (direction = 1) or right (direction = -1)!");

        targetAngle = -90f * direction; // -1 * -90 = +90 goes clockwise (right), +1 * -90 = -90 goes anti-clockwise (left).
        elapsed = 0f;
        isFalling = true;
    }

    private void Update()
    {
        if (!isFalling) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / fallDuration);
        float angle = fallCurve.Evaluate(t) * targetAngle;

        // Snap to discrete steps so that the sprite resamples consistently in pixel art.
        angle = Mathf.Round(angle / angleStep) * angleStep;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (t >= 1f) isFalling = false;
    }
}
