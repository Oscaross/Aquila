using UnityEngine;

/// <summary>
/// A body of water. Owns the authoritative surface height — everything that
/// needs the waterline reads it from here rather than storing its own copy.
/// </summary>
[ExecuteAlways]
public class WaterBody : MonoBehaviour
{
    /// <summary>World-space y of the water surface, snapped to the pixel grid.</summary>
    public float SurfaceY => Mathf.Round(transform.position.y * GlobalConstants.PixelsPerUnit) / GlobalConstants.PixelsPerUnit;
}