using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles the zoning logic for the world, including which zone is where and what, the bounds of each and the global world bounds. Single authority on all zoning lives here through these APIs.
/// Execution order is -100 so it happens before other classes because it's the main API they query, so it is important that the zones have been instantiated before other scripts run like the
/// tilemap painter otherwise the world generates incorrectly.
/// </summary>

[DefaultExecutionOrder(-100)]
public class ZoneManager : GameManagerComponent
{
    [Tooltip("A prefab instance of each specific zone, such as the forest zone prefab or the arable zone prefab.")]
    [SerializeField] private Zone[] zonePrefabs;
    private readonly List<Zone> zones = new();
    // A stable mapping from relative world coordinate => zone type for each integer position in the world.
    // Index 0 is the minimum x position, so in a world that is 100 wide, idx = 0 represents -50 and idx = 99 represents +50.
    private ZoneType[] typesAtCoords;
    
    /// <summary>
    /// The bounds of the area of the world walkable to the player from left to right.
    /// </summary>
    public RectInt WorldBounds => worldBounds;

    [SerializeField] private RectInt worldBounds;
    
    // private void Awake()
    // {
    //     CreateNewZone(new RectInt(new Vector2Int(-40, 0), new Vector2Int(80, 10)), ZoneType.Legion);
    //     CreateNewZone(new RectInt(new Vector2Int(-150, 0), new Vector2Int(100, 10)), ZoneType.Arable);
    //     CreateNewZone(new RectInt(new Vector2Int(70, 0), new Vector2Int(55, 10)), ZoneType.Forest);
    // }

    public override void Restore(WorldState state)
    {

    }

    public RectInt GetLegionBounds()
    {
        var matching = new List<Zone>();
        
        foreach (Zone z in zones)
        {
            if (z.Type == ZoneType.Legion) matching.Add(z);
        }

        if (matching.Count == 0)
        {
            Debug.LogError("No legion zone exists! The game must have one zone of type Legion.", this);
        }

        if (matching.Count > 1)
        {
            Debug.LogError($"{matching.Count} legion zones exist! The legion can only have one contiguous legion zone.", this);
        }

        return matching.First().Bounds;
    }

    private void CreateNewZone(RectInt bounds, ZoneType type)
    {
        // Check this zone doesn't overlap with an existing one
        foreach (Zone existing in zones)
            if (bounds.xMin < existing.Bounds.xMax && existing.Bounds.xMin < bounds.xMax)
            {
                Debug.LogError($"{name}: {type} at [{bounds.xMin},{bounds.xMax}) overlaps " +
                               $"{existing.Type} at [{existing.Bounds.xMin},{existing.Bounds.xMax}).", this);
                return;
            }
        
        if (bounds.xMin < WorldBounds.xMin || bounds.xMax > WorldBounds.xMax)
        {
            Debug.LogError("Can't have a zone that is outside of the world area!");
            return;
        }

        Zone newZone = null;
        
        foreach (Zone z in zonePrefabs)
        {
            if (z.Type == type)
            {
                newZone = Instantiate(z, new Vector3(bounds.position.x, bounds.position.y, 0f), Quaternion.identity, transform);
                break;
            }
        }

        if (newZone == null)
        {
            Debug.LogError($"No prefab exists for the zone of type {type.ToString()}!", this);
            return;
        }
        
        // Zone exists and has been created so configure it
        // newZone.Initialise(bounds);
        newZone.OnZoneCreated();
        
        zones.Add(newZone);
        zones.Sort((a, b) => a.Bounds.xMin.CompareTo(b.Bounds.xMin));
        RebuildCoordinateList();
    }

    /// <summary>
    /// Checks whether a rectangular area is fully contained within the bounds of a zone of type.
    /// </summary>
    /// <param name="bounds">The rectangular area to check within, in world coordinates.</param>
    /// <param name="type">The zone type to search for.</param>
    /// <returns>True if it fully is contained within a zone of type, false otherwise. Returns false if the bounds have zero-width.</returns>
    public bool DoesAreaFullyOverlapType(RectInt bounds, ZoneType type)
    {
        if (bounds.width <= 0) return false;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            if (GetZoneTypeAt(x) != type) return false;
        }

        return true;
    }

    private void RemoveZone(Zone existing)
    {
        existing.OnZoneDestroyed();
        zones.Remove(existing);
        
        Destroy(existing);
        RebuildCoordinateList();
    }

    /// <summary>
    /// Populates the typesAtCoords list after we add or subtract a zone.
    /// Each index represents a world position, with the smallest position being idx = 0 and the largest being the idx = maxArea - 1.
    /// Each index stores the type of the zone.
    /// </summary>
    private void RebuildCoordinateList()
    {
        if (WorldBounds.width <= 0)
        {
            Debug.LogError($"{name}: worldBounds has zero width. Set it in the inspector.", this);
            return;
        }
        
        typesAtCoords = new ZoneType[WorldBounds.width];
        
        // Populate a ZoneType value for each of our worldBounds.width integer tile positions.
        for (int i = 0; i < typesAtCoords.Length; i++)
        {
            int worldX = WorldXOf(i);
            bool isWilderness = true;

            foreach (Zone zone in zones)
                if (zone.Bounds.Contains(new Vector2Int(worldX, zone.Bounds.yMin)))
                {
                    typesAtCoords[i] = zone.Type;
                    isWilderness = false;
                    break;
                }

            if (isWilderness) typesAtCoords[i] = ZoneType.Wilderness;
        }
    }
    
    public List<Zone> GetAllZones() => zones;

    private int IndexOf(float worldX) => Mathf.FloorToInt(worldX) - WorldBounds.xMin; // floor because we don't want something partially into the 4 tile to round up to the 5 tile.

    private int WorldXOf(int index) => WorldBounds.xMin + index;
    
    /// <summary>
    /// Takes an x coordinate in world coordinates and returns the zone type that the coordinate lies inside.
    /// </summary>
    /// <param name="x">The x coordinate to sample.</param>
    /// <returns>The ZoneType. ZoneType.Wilderness is the default return type and will be returned in the case that this object does not lie in a predefined zone.</returns>
    public ZoneType GetZoneTypeAt(float x)
    {
        return typesAtCoords[IndexOf(x)];
    }

    private void OnDrawGizmos()
    {
        foreach (Zone zone in zones)
        {
            RectInt r = zone.Bounds;
            var centre = new Vector3(r.xMin + r.width * 0.5f, r.yMin + r.height * 0.5f, 0f);
            var size   = new Vector3(r.width, r.height, 0f);

            Color c = ColourFor(zone.Type);

            Gizmos.color = new Color(c.r, c.g, c.b, 0.15f);
            Gizmos.DrawCube(centre, size);

            Gizmos.color = c;
            Gizmos.DrawWireCube(centre, size);

            #if UNITY_EDITOR
                        UnityEditor.Handles.color = c;
                        UnityEditor.Handles.Label(new Vector3(r.xMin, r.yMax + 0.5f, 0f),
                            $"{zone.Type}  {r.width}×{r.height}");
            #endif
        }
    }

    private static Color ColourFor(ZoneType type) => type switch
    {
        ZoneType.Arable => Color.yellow,
        ZoneType.Forest => Color.green,
        ZoneType.Legion => Color.red,
        ZoneType.Wilderness => Color.cyan,
        _ => Color.magenta
    };
}
