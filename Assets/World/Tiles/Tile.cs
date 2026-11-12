using UnityEngine;

[CreateAssetMenu(fileName = "Tile", menuName = "Scriptable Objects/Worldgen/Tile")]
public class Tile : ContentAsset
{
    [Tooltip("Unique group that this tile is a part of. For example, a rocky lake scene group.")]
    public TileGroup group;
    [Tooltip("The prefab instance that will be spawned when we spawn this tile.")]
    public GameObject prefab;
    [Tooltip("The zone that this tile occurs in.")]
    public ZoneType zone;
    [Tooltip("The position in the zone's z-stack that this tile is occupying. For example, is it the ground layer the player walks on, or is it a mid background layer?")]
    public TileLayer type;
    [Tooltip("Constraints that must be satisfied for this tile to be placed in the world.")]
    public TileConstraint[] constraints;
} 