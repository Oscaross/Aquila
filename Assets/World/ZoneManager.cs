using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the zoning logic for the world, including which zone is where and what, the bounds of each and the global world bounds. Single authority on all zoning lives here through these APIs.
/// Execution order is -100 so it happens before other classes because it's the main API they query, so it is important that the zones have been instantiated before other scripts run like the
/// tilemap painter otherwise the world generates incorrectly.
/// </summary>

[DefaultExecutionOrder(-100)]
public class ZoneManager : MonoBehaviour
{
    private List<Zone> zones = new();
    // A stable mapping from relative world coordinate => zone type for each integer position in the world.
    // Index 0 is the minimum x position, so in a world that is 100 wide, idx = 0 represents -50 and idx = 99 represents +50.
    private ZoneType[] typesAtCoords;
    
    /// <summary>
    /// The bounds of the area of the world walkable to the player from left to right.
    /// </summary>
    public RectInt worldBounds;

    private void Awake()
    {
        CreateNewZone(new RectInt(new Vector2Int(-40, 0), new Vector2Int(80, 10)), ZoneType.Legion);
        CreateNewZone(new RectInt(new Vector2Int(-90, 0), new Vector2Int(50, 10)), ZoneType.Arable);
        CreateNewZone(new RectInt(new Vector2Int(70, 0), new Vector2Int(55, 10)), ZoneType.Forest);
    }

    private void CreateNewZone(RectInt bounds, ZoneType type)
    {
        // Check this zone doesn't overlap with an existing one
        foreach (Zone existing in zones)
            if (bounds.xMin < existing.bounds.xMax && existing.bounds.xMin < bounds.xMax)
            {
                Debug.LogError($"{name}: {type} at [{bounds.xMin},{bounds.xMax}) overlaps " +
                               $"{existing.type} at [{existing.bounds.xMin},{existing.bounds.xMax}).", this);
                return;
            }
        
        if (bounds.xMin < worldBounds.xMin || bounds.xMax > worldBounds.xMax)
        {
            Debug.LogError("Can't have a zone that is outside of the world area!");
            return;
        }
        
        zones.Add(new Zone(bounds, type));
        zones.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
        RebuildCoordinateList();
    }

    private void RemoveZone(Zone existing)
    {
        zones.Remove(existing);
        RebuildCoordinateList();
    }

    /// <summary>
    /// Populates the typesAtCoords list after we add or subtract a zone.
    /// Each index represents a world position, with the smallest position being idx = 0 and the largest being the idx = maxArea - 1.
    /// Each index stores the type of the zone.
    /// </summary>
    private void RebuildCoordinateList()
    {
        if (worldBounds.width <= 0)
        {
            Debug.LogError($"{name}: worldBounds has zero width. Set it in the inspector.", this);
            return;
        }
        
        typesAtCoords = new ZoneType[worldBounds.width];
        
        // Populate a ZoneType value for each of our worldBounds.width integer tile positions.
        for (int i = 0; i < typesAtCoords.Length; i++)
        {
            int worldX = WorldXOf(i);
            bool isWilderness = true;

            foreach (Zone zone in zones)
                if (zone.bounds.Contains(new Vector2Int(worldX, zone.bounds.yMin)))
                {
                    typesAtCoords[i] = zone.type;
                    isWilderness = false;
                    break;
                }

            if (isWilderness) typesAtCoords[i] = ZoneType.Wilderness;
        }
    }

    /// <summary>
    /// Returns a contiguous block of zones matching the current zone from the minimum to maximum x coordinate of the world.
    /// This method returns set of zones A, such that the intersection of all zones RectInt in A is precisely the set of all possible integer coordinates in the world.
    /// Zones are returned in ascending order, meaning the smallest x coordinates are the first in the list and the largest the last in the list.
    /// </summary>
    /// <returns></returns>
    public List<Zone> GetAllZones()
    {
        List<Zone> ret = new();
        if (typesAtCoords == null || typesAtCoords.Length == 0) return ret;

        ZoneType lastZoneType = typesAtCoords[0];
        int lastZoneStartX = WorldXOf(0);

        for (int i = 1; i < typesAtCoords.Length; i++)
        {
            if (typesAtCoords[i] == lastZoneType) continue;

            int worldX = WorldXOf(i);
            ret.Add(new Zone(
                new RectInt(new Vector2Int(lastZoneStartX, 0),
                    new Vector2Int(worldX - lastZoneStartX, worldBounds.height)),
                lastZoneType));

            lastZoneType = typesAtCoords[i];
            lastZoneStartX = worldX;
        }
        
        int endX = WorldXOf(typesAtCoords.Length);
        ret.Add(new Zone(
            new RectInt(new Vector2Int(lastZoneStartX, 0),
                new Vector2Int(endX - lastZoneStartX, worldBounds.height)),
            lastZoneType));

        ret.Sort((a, b) => a.bounds.xMin.CompareTo(b.bounds.xMin));
        
        return ret;
    }

    private int IndexOf(float worldX) => Mathf.FloorToInt(worldX) - worldBounds.xMin; // floor because we don't want something partially into the 4 tile to round up to the 5 tile.

    private int WorldXOf(int index) => worldBounds.xMin + index;
    
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
            RectInt r = zone.bounds;
            var centre = new Vector3(r.xMin + r.width * 0.5f, r.yMin + r.height * 0.5f, 0f);
            var size   = new Vector3(r.width, r.height, 0f);

            Color c = ColourFor(zone.type);

            Gizmos.color = new Color(c.r, c.g, c.b, 0.15f);
            Gizmos.DrawCube(centre, size);

            Gizmos.color = c;
            Gizmos.DrawWireCube(centre, size);

            #if UNITY_EDITOR
                        UnityEditor.Handles.color = c;
                        UnityEditor.Handles.Label(new Vector3(r.xMin, r.yMax + 0.5f, 0f),
                            $"{zone.type}  {r.width}×{r.height}");
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
