using UnityEngine;

/**
 * Calculates and applies the correct gradient for the sky depending on the time of day. 
*/

[ExecuteAlways]
public class SkyColour : MonoBehaviour
{
    [SerializeField] private SpriteRenderer zenith;

    private void LateUpdate()
    {
        var sky = SkyController.Instance;
        if (sky == null) return;

        zenith.color = sky.Zenith;
    }
}