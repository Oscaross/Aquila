using UnityEngine;

[CreateAssetMenu(fileName = "BuildingConstraints", menuName = "Scriptable Objects/Building/Building Constraints")]
public class BuildingConstraints : ScriptableObject
{
    public GameObject buildingPrefab;
    public int width;
    public ZoneType[] zonesBuildableIn;
    public ResourceTransaction[] resourceCosts;
}
