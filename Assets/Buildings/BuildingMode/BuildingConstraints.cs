using UnityEngine;

[CreateAssetMenu(fileName = "BuildingConstraints", menuName = "Scriptable Objects/Building/Building Constraints")]
public class BuildingConstraints : ScriptableObject
{
    [Tooltip("A bottom-centre aligned instance of the building that should be spawned into the world.")]
    public GameObject buildingPrefab;
    [Tooltip("In world units.")]
    public int width;
    [Tooltip("In world units.")]
    public int height;
    [Tooltip("This building may only be constructed if its footprint FULLY OVERLAPS these zones.")]
    public ZoneType[] zonesBuildableIn;
    public ResourceTransaction[] resourceCosts;
    [Tooltip("Rules that dictate which other buildings this building must be built within a certain range of, either closer than some value or further than some value. All constraints must be met for the player to place the building.")]
    public ProximityRule[] proximityRules;
}
