using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the zoning logic for the world, including which zone is where and what, the bounds of each and the global world bounds. Single authority on all zoning lives here through these APIs.
/// </summary>

public class ZoneManager : GameManagerComponent
{
    private List<Zone> zoneInstances = new();
    [SerializeField] private Zone prefab;
    private EntityStore<ZoneModel> Zones => save.ZoneState.Zones;
    
    /// <summary>
    /// The bounds of the area of the world walkable to the player from left to right.
    /// </summary>
    public RectInt WorldBounds => worldBounds;

    [SerializeField] private RectInt worldBounds;

    private WorldState save;
    private WorldContext context;

    public override void Restore(WorldState state, WorldContext ctx)
    {
        save = state;
        context = ctx;
        
        foreach (ZoneModel data in state.ZoneState.Zones.Items)
        {
            SetupZone(data);
        }
    }

    public override void GenerateNewWorld()
    {
        CreateNewZone(new RectInt(-50, 0, 100, 10), ZoneType.Legion);
        CreateNewZone(new RectInt(-100, 0, 40, 10), ZoneType.Arable);
        CreateNewZone(new RectInt(50, 0, 70, 10), ZoneType.Forest);
    }

    /// <summary>
    /// Creates a zone object and saves it. 
    /// </summary>
    /// <param name="bounds">The bounding box of this zone, a rectangle with integer height, width and vertices.</param>
    /// <param name="type">The type of the zone i.e. Legion, Arable, Forest...</param>
    public void CreateNewZone(RectInt bounds, ZoneType type)
    {
        if (!ValidateZone(bounds, type)) return;
        
        var data = new ZoneModel(type, bounds.width, bounds.height, bounds.xMin, bounds.yMin);
        Zones.Add(data, save.IdAllocator);
        
        SetupZone(data);
    }

    // Creates a zone from an existing data template
    private void SetupZone(ZoneModel data)
    {
        Zone z = Instantiate(prefab, new Vector3(data.Bounds.xMin, data.Bounds.yMin, 0f), Quaternion.identity,
            transform);
        z.Bind(data);
        context.Registry.Register(z);
        
        zoneInstances.Add(z);
        zoneInstances.Sort((a, b) => a.Bounds.xMin.CompareTo(b.Bounds.xMin)); // sort ascending so most -ve rectangular zones are first and most +ve are last
    }
    
    private bool ValidateZone(RectInt bounds, ZoneType type)
    {
        // Check this zone doesn't overlap with an existing one
        foreach (ZoneModel existing in Zones.Items)
            if (bounds.xMin < existing.Bounds.xMax && existing.Bounds.xMin < bounds.xMax)
            {
                Debug.LogError($"{name}: {type} at [{bounds.xMin},{bounds.xMax}) overlaps " +
                               $"{existing.Type} at [{existing.Bounds.xMin},{existing.Bounds.xMax}).", this);
                return false;
            }
        
        // Check this zone doesn't exit the world area
        if (bounds.xMin < WorldBounds.xMin || bounds.xMax > WorldBounds.xMax)
        {
            Debug.LogError("Can't have a zone that is outside of the world area!");
            return false;
        }

        return true;
    }


    public RectInt GetLegionBounds()
    {
        var matching = new List<ZoneModel>();
        
        foreach (ZoneModel z in Zones.Items)
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
    
    /// <summary>
    /// Checks whether a rectangular area is fully contained within the bounds of a zone of type.
    /// </summary>
    /// <param name="bounds">The rectangular area to check within, in world coordinates.</param>
    /// <param name="type">The zone type to search for.</param>
    /// <returns>True if it fully is contained within a zone of type, false otherwise. Returns false if the bounds have zero-width.</returns>
    public bool DoesAreaFullyOverlapType(RectInt bounds, ZoneType type)
    {
        if (bounds.width <= 0) return false;
        
        // Loop over each discrete integer step in bounds, i.e. a 5 wide rectangle we check x = 0, 1, .. 4
        // If each of the integer cells lies in the desired zone, then we're fully overlapping. Else, we are not.
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            if (GetZoneTypeAt(x) != type) return false;
        }

        return true;
    }

    public ZoneType GetZoneTypeAt(int x) => GetZoneTypeAt(x, GlobalConstants.GroundY);

    public ZoneType GetZoneTypeAt(int x, int y)
    {
        foreach (Zone z in zoneInstances)
        {
            var pos = new Vector2Int(x, y);
            if (z.Bounds.Contains(pos)) return z.Type;
        }

        return ZoneType.Wilderness; // wilderness is the default
    }

    public IReadOnlyList<Zone> GetAllZones() => zoneInstances;

    private void OnDrawGizmos()
    {
        if (save == null) return;
        
        foreach (ZoneModel zone in Zones.Items)
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
