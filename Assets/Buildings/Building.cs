using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] private BuildingConstraints constraints;
    public BuildingConstraints Constraints => constraints;

    private BuildingManager buildingManager;
    private Legion legion;

    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
        buildingManager = legion.BuildingManager;
    }

    public void Register(RectInt footprintInWorldSpace)
    {
        FootprintInWorldSpace = footprintInWorldSpace;
        buildingManager.Register(this);
    }

    public RectInt FootprintInWorldSpace
    {
        get;
        private set;
    }
}
