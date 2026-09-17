using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for allowing the player to place buildables into the world and see holograms
/// </summary>

public class BuildingModeManager : MonoBehaviour
{
    private Legion legion;
    private LegionResources resources;
    private ZoneManager zoneManager;
    [SerializeField] private BuildingConstraints[] allBuildables;
    [Tooltip("How many world units in front of the player the building hologram should be rendered.")]
    [SerializeField] private int hologramOffsetWorldUnits;
    
    private bool isBuildingMode;
    // Cell is the last integer cell the player was stood at, facing is the last direction they were facing.
    // If either change, we need to rebuild the hologram and reevaluate what buildables they're able to construct.
    private (int cell, int facing) buildingContext = (int.MinValue, 1); // cell HAS to be the min value so we don't initialise to 0 then claim that context is the same
    // If this flag is true it triggers a reevaluation of valid buildables at this position.
    private bool hologramDirty;
    private readonly List<BuildingConstraints> currentBuildableHolograms = new();
    // Every building is pre-generated as a cached game object so we don't need to re-instantiate each time the hologram state changes
    private readonly Dictionary<BuildingConstraints, GameObject> hologramCache = new();
    private BuildingConstraints currentBuildableSelected;
    private GameObject activeHologram;
    private RectInt currentFootprint;
    private List<RectInt> placedFootprints = new();
    
    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
        resources = legion.Resources;
        zoneManager = legion.ZoneManager;

        if (allBuildables == null || allBuildables.Length == 0)
        {
            Debug.LogError("Warning: the building manager has no building assets available to it. The building system will not work properly.");
        }
    }
    
    /// <summary>
    /// Works out the current cell the player is standing in, and the direction they are facing.
    /// </summary>
    /// <returns>A context tuple, containing the cell index the player is standing in and the signed direction value for the direction they are facing.</returns>
    private (int cell, int facing) GetContext()
    {
        int cell = Mathf.FloorToInt(legion.Player.transform.position.x);
        int facing = legion.Player.InputManager.FacingDirection.Sign() >= 0 ? 1 : -1;
        return (cell, facing);
    }
    
    /// <summary>
    /// Determines the rectangular footprint area occupied by an instance of this building.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private RectInt GetFootprint((int cell, int facing) ctx, BuildingConstraints b)
    {
        int originX = ctx.facing > 0
            ? ctx.cell + 1 + hologramOffsetWorldUnits // right of the player's cell
            : ctx.cell - hologramOffsetWorldUnits - b.width; // left of the player's cell
        return new RectInt(originX, GlobalConstants.GroundY, b.width, b.height);
    }
    
    private static Vector2 FootprintToWorld(RectInt fp) => new(fp.center.x, fp.yMin);

    private void LateUpdate()
    {
        if (!isBuildingMode) return;
        (int, int) currentContext = GetContext();
        
        if (buildingContext != currentContext)
        {
            buildingContext = currentContext;
            hologramDirty = true; // cell position we're in changed => hologram needs re-evaluating 
        }

        if (!hologramDirty) return;
        hologramDirty = false;
        
        DetermineLegalBuildables();
        DrawHologram();
    }

    public void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;
        if (isBuildingMode) hologramDirty = true;

        if (!isBuildingMode && activeHologram != null) activeHologram.SetActive(false); // deactivate old holograms when we exit build mode
    }

    private void DrawHologram()
    {
        if (activeHologram != null) activeHologram.SetActive(false);
        if (currentBuildableSelected == null) return;

        activeHologram = GetHologram(currentBuildableSelected);
        activeHologram.transform.position = FootprintToWorld(currentFootprint);
        activeHologram.SetActive(true);
    }

    /// <summary>
    /// Re-evaluates the available holograms to the user at this position and writes these valid buildables into the class hologram list.
    /// </summary>
    private void DetermineLegalBuildables()
    {
        currentBuildableHolograms.Clear();
        foreach (var b in allBuildables)
            if (Validate(GetFootprint(buildingContext, b), b) == BuildingFailureReason.None)
                currentBuildableHolograms.Add(b);

        if (!currentBuildableHolograms.Contains(currentBuildableSelected))
            currentBuildableSelected = currentBuildableHolograms.Count > 0 ? currentBuildableHolograms[0] : null;

        if (currentBuildableSelected != null)
            currentFootprint = GetFootprint(buildingContext, currentBuildableSelected);
    }

 
    private GameObject GetHologram(BuildingConstraints constraints)
    {
        // Check the cache, on hit return, on miss populate the cache with this building type for future use.
        if (!hologramCache.TryGetValue(constraints, out GameObject hologram))
        {
            hologram = Instantiate(constraints.buildingPrefab, transform);
            // *IMPORTANT* we MUST deactivate all behavioural components of this hologram, otherwise, it'll consider any instantiated but unplaced hologram a real building and cause game-breaking bugs
            foreach (Behaviour b in hologram.GetComponentsInChildren<Behaviour>(true))
            {
                b.enabled = false;
            }

            hologramCache[constraints] = hologram;
            hologram.SetActive(false); // any newly created cached hologram can't be active or visible, that isn't our job here
        }
        
        return hologram;
    }
    
    private BuildingFailureReason Validate(RectInt fp, BuildingConstraints b)
    {
        if (!IsInZone(fp, b)) return BuildingFailureReason.NotInZone;
        if (OverlapsExisting(fp, b)) return BuildingFailureReason.TooCloseToAnother;
        foreach (ResourceTransaction rt in b.resourceCosts)
            if (!resources.CanConsumeResource(rt.resource, rt.amount))
                return BuildingFailureReason.InsufficientResources;
        return BuildingFailureReason.None;
    }

    // TODO: For now we'll just allow overlaps, once we get it working, we'll add these back.
    private bool OverlapsExisting(RectInt fp, BuildingConstraints b)
    {
        return false;
    }

    private bool IsInZone(RectInt fp, BuildingConstraints b)
    {
        foreach (ZoneType z in b.zonesBuildableIn)
        {
            if (zoneManager.DoesAreaFullyOverlapType(fp, z)) return true;
        }

        return false;
    }

    public bool TryBuild()
    {
        if (!isBuildingMode || currentBuildableSelected == null) return false;

        var b = currentBuildableSelected;
        RectInt fp = GetFootprint(GetContext(), b);   // fresh, not last frame's
        if (Validate(fp, b) != BuildingFailureReason.None) return false;

        Instantiate(b.buildingPrefab, FootprintToWorld(fp), Quaternion.identity, transform);
        foreach (ResourceTransaction rt in b.resourceCosts)
            resources.ConsumeResource(rt.resource, rt.amount);

        placedFootprints.Add(fp);
        hologramDirty = true;
        return true;
    }
    
    
    private void OnDrawGizmos()
    {
        // draw a cyan box around the footprint of the current buildable to verify that it is being drawn in the correct place
        if (!isBuildingMode || currentBuildableSelected == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(currentFootprint.center, new Vector3(currentFootprint.width, currentFootprint.height));
    }
}