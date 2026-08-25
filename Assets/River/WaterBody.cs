using UnityEngine;

[ExecuteAlways]
public class WaterBody : MonoBehaviour
{
    [SerializeField] private int pixelsPerUnit = 16;
    [SerializeField] private SpriteRenderer plane;

    /// <summary>World-space y of the water surface (the plane's top edge), snapped to the pixel grid.</summary>
    public float SurfaceY
    {
        get
        {
            float top = plane.bounds.max.y;
            return Mathf.Round(top * pixelsPerUnit) / pixelsPerUnit;
        }
    }
}