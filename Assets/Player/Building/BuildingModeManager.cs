using System.Collections.Generic;
using UnityEngine;

public class BuildingModeManager : MonoBehaviour
{
    private Legion legion;
    private LegionResources resources;
    private ZoneManager zoneManager;
    [SerializeField] private BuildingConstraints[] allBuildables;
    
    private bool isBuildingMode;
    // The last x position we rendered the hologram at so we can re-evaluate the hologram on moving.
    private int lastCell = int.MinValue; // if this were 0 it'd never run at the origin if it were the first time opening the build menu

    private bool hologramDirty;
    private List<BuildingConstraints> currentBuildableHolograms = new();
    // Every building is pre-generated as a cached game object so we don't need to re-instantiate each time the hologram state changes
    private readonly Dictionary<BuildingConstraints, GameObject> hologramCache = new();
    private BuildingConstraints currentBuildableSelected;
    private GameObject activeHologram;
    
    
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

    private void LateUpdate()
    {
        if (!isBuildingMode) return;
        
        int currentCell = Mathf.FloorToInt(legion.Player.transform.position.x);
        if (currentCell != lastCell)
        {
            lastCell = currentCell;
            hologramDirty = true; // cell position we're in changed => hologram needs re-evaluating 
        }

        if (!hologramDirty) return;
        hologramDirty = false;
        
        DetermineLegalBuildables(currentCell);
        DrawHologram(currentCell);
    }

    public void Build(BuildingConstraints building, Vector2 buildPos)
    {
        // Has to pass final validation before we build.
        if (Validate(lastCell, currentBuildableSelected) != BuildingFailureReason.None) return; 
        
        Instantiate(building.buildingPrefab, buildPos, Quaternion.identity, transform);
        
        foreach (ResourceTransaction rt in building.resourceCosts)
        {
            resources.ConsumeResource(rt.resource, rt.amount);
        }
    }

    public void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;
        if (isBuildingMode) hologramDirty = true;

        if (!isBuildingMode && activeHologram != null) activeHologram.SetActive(false); // deactivate old holograms when we exit build mode
        
        Debug.Log((isBuildingMode) ? "Entering building mode." : "Exiting building mode.");
    }

    private void DrawHologram(int cellPos)
    {
        if (activeHologram != null) activeHologram.SetActive(false);
        if (currentBuildableSelected == null) return;
        
        activeHologram = GetHologram(currentBuildableSelected);
        activeHologram.SetActive(true);
        activeHologram.transform.position = new Vector2(cellPos, activeHologram.transform.position.y);
    }

    /// <summary>
    /// Re-evaluates the available holograms to the user at this position and writes these valid buildables into the class hologram list.
    /// </summary>
    private void DetermineLegalBuildables(int cellPos)
    {
        currentBuildableHolograms.Clear();
        
        foreach (BuildingConstraints constraint in allBuildables)
        {
            if (Validate(cellPos, constraint) == BuildingFailureReason.None) currentBuildableHolograms.Add(constraint);
        }
        
        // Verify that our current selected buildable is actually valid with the new legal buildables
        if (!currentBuildableHolograms.Contains(currentBuildableSelected))
        {
            currentBuildableSelected = currentBuildableHolograms.Count > 0 ? currentBuildableHolograms[0] : null;
        }
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

    private BuildingFailureReason Validate(int cellX, BuildingConstraints constraints)
    {
        if (!IsBuildingInZone(cellX, constraints)) return BuildingFailureReason.NotInZone;
        if (IsTooCloseToAnother(cellX, constraints)) return BuildingFailureReason.TooCloseToAnother;

        foreach (ResourceTransaction rt in constraints.resourceCosts)
        {
            if (!resources.CanConsumeResource(rt.resource, rt.amount))
                return BuildingFailureReason.InsufficientResources;
        }

        return BuildingFailureReason.None;
    }

    private bool IsTooCloseToAnother(int cellX, BuildingConstraints constraints)
    {
        return false;
    }

    private bool IsBuildingInZone(int worldPosX, BuildingConstraints constraints)
    {
        Vector2Int buildingOrigin = new Vector2Int(Mathf.FloorToInt(worldPosX - constraints.width / 2f), 0); // the origin is the bottom left corner, which is width/2 to the left of the attempted spawn pos
        RectInt boundsInWorldSpace = new RectInt(buildingOrigin, new Vector2Int(constraints.width, 10));
        
        foreach (ZoneType t in constraints.zonesBuildableIn)
        {
            if (zoneManager.DoesAreaFullyOverlapType(boundsInWorldSpace, t))  return true;
        }

        return false;
    }
}