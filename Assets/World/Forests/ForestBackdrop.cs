using UnityEngine;

public class ForestBackdrop : MonoBehaviour
{
    [SerializeField] private Sprite forestBackgroundMaterial;

    private SpriteRenderer sr;
    
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Initialise(RectInt bounds)
    {
        sr.bounds = new Bounds(bounds.center, new Vector3(bounds.size.x, bounds.size.y, 0));
    }
}
