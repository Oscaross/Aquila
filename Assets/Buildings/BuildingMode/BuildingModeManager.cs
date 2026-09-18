using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Responsible for allowing the player to place buildables into the world and see holograms
/// </summary>

public class BuildingModeManager : MonoBehaviour
{
    [SerializeField] private BuildingConstraints[] allBuildables;
    // ALWAYS INACTIVE! All holograms MUST be created as children of staging otherwise their awake scripts which will run their awake logic and consider them live assets! (This sends that bug back to hell where it belongs.)
    [SerializeField] private Transform hologramStaging;
    // All inactive holograms live here. It is active so an active hologram is visible, but that doesn't matter as the game object's components are already safely off.
    [SerializeField] private Transform hologramRoot;
    
    private Legion legion;
    private LegionResources resources;
    private ZoneManager zoneManager;
    private BuildingManager buildingManager;
    private Camera cam;
    
    #region Logic
    private bool isBuildingMode;
    private int currentBuildCell = int.MinValue; // cell HAS to be the min value so we don't initialise to 0 then claim that context is the same
    // If this flag is true it triggers a reevaluation of valid buildables at this position.
    private bool hologramDirty;
    private readonly List<BuildingConstraints> currentBuildableHolograms = new();
    // Every building is pre-generated as a cached game object so we don't need to re-instantiate each time the hologram state changes
    private readonly Dictionary<BuildingConstraints, GameObject> hologramCache = new();
    private BuildingConstraints currentBuildableSelected;
    private GameObject activeHologram;
    private RectInt currentFootprint;
    private int hologramSelectionIdx;
    #endregion

    #region Input
    private InputActionMap buildingInputMap;
    private InputActionMap gameplayInputMap;

    private InputAction pointer; // the mouse/joystick that the player uses to move the hologram around
    private InputAction toggleBuildMode;
    private InputAction placeBuilding;
    private InputAction cycleHologramUp;
    private InputAction cycleHologramDown;
    #endregion
    
    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
        resources = legion.Resources;
        zoneManager = legion.ZoneManager;
        buildingManager = legion.BuildingManager;
        cam = Camera.main;

        RegisterInputSystem();

        ValidateBuildables();

        if (allBuildables == null || allBuildables.Length == 0)
        {
            Debug.LogError("Warning: the building manager has no building assets available to it. The building system will not work properly.");
        }
    }

    private void ValidateBuildables()
    {
        if (allBuildables == null || allBuildables.Length == 0)
        {
            Debug.LogError("Warning: the building manager has no building assets available to it. The building system will not work properly.");
            return;
        }

        foreach (BuildingConstraints b in allBuildables)
        {
            var prefab = b.buildingPrefab;
            if (prefab == null)
            {
                Debug.LogError($"Missing prefab on buildable {b.name}");
                return;
            }

            if (prefab.GetComponent<Building>() == null)
            {
                Debug.LogError($"Prefab is incorrectly configured for {b.name}, missing a Building script on the prefab. You must add this script.");
            }
        }
    }

    private void RegisterInputSystem()
    {
        buildingInputMap = InputSystem.actions.FindActionMap("Building", true); // the map contains all of our keybindings for the Building control map
        gameplayInputMap = InputSystem.actions.FindActionMap("Gameplay", true); // for build mode only
        
        toggleBuildMode = gameplayInputMap.FindAction("ToggleBuildMode", true);
        pointer = buildingInputMap.FindAction("MoveHologram", true);
        placeBuilding = buildingInputMap.FindAction("PlaceBuilding", true);
        cycleHologramUp = buildingInputMap.FindAction("CycleHologramUp", true);
        cycleHologramDown = buildingInputMap.FindAction("CycleHologramDown", true);
    }

    /// Input Logic:
    private void OnEnable()
    {
        toggleBuildMode.performed += ToggleBuildMode;
        placeBuilding.performed += TryBuild;
        cycleHologramUp.performed += CycleHologramUp;
        cycleHologramDown.performed += CycleHologramDown;
    }

    private void OnDisable()
    {
        toggleBuildMode.performed -= ToggleBuildMode;
        placeBuilding.performed -= TryBuild;
        cycleHologramUp.performed -= CycleHologramUp;
        cycleHologramDown.performed -= CycleHologramDown;
    }

    private void ToggleBuildMode(InputAction.CallbackContext _)
    {
        if (isBuildingMode) buildingInputMap.Disable();
        else  buildingInputMap.Enable();
        
        isBuildingMode = !isBuildingMode;
        if (isBuildingMode) hologramDirty = true;

        if (!isBuildingMode && activeHologram != null) activeHologram.SetActive(false); // deactivate old holograms when we exit build mode
    }

    private void CycleHologramUp(InputAction.CallbackContext _) => CycleHologram(1);
    private void CycleHologramDown(InputAction.CallbackContext _) => CycleHologram(-1);

    // +1 to go up the list, -1 to go back down
    private void CycleHologram(int incrementAmount)
    {
        int n = currentBuildableHolograms.Count;
        if (n == 0) return; // no point cycling through a list of no holograms
        
        hologramSelectionIdx = ((hologramSelectionIdx + incrementAmount) % n + n) % n; // up increment goes 0, 1, 2, n-1, n, 0, ... and down the opposite
        
        ApplySelection();
        hologramDirty = true; // we'll need to reevaluate the hologram if it changed
    }
    
    private void TryBuild(InputAction.CallbackContext _)
    {
        if (!isBuildingMode || currentBuildableSelected == null) return;

        var b = currentBuildableSelected;
        RectInt fp = GetFootprint(GetCurrentCell(), b); // fresh, not last frame's
        if (!IsValidBuildable(fp, b)) return; // This isn't a valid buildable anymore so we can't build it

        var go = Instantiate(b.buildingPrefab, FootprintToWorld(fp), Quaternion.identity, transform);
        
        if (!go.TryGetComponent(out Building building))
        {
            Debug.LogError($"The building prefab for {b.name} has no Building component attached. Fix the prefab!", this);
            Destroy(go);
            return;
        }
        
        hologramDirty = true;
        building.Register(fp);
        
        foreach (ResourceTransaction rt in b.resourceCosts)
            resources.ConsumeResource(rt.resource, rt.amount);
    }
    
    /// System Logic:

    /// Fetches the position of the cursor to the nearest whole cell number so that the hologram can be placed there.
    private int GetCurrentCell()
    {
        Vector2 screenMousePos = pointer.ReadValue<Vector2>(); // fetch the screen coordinates of the pointer (i.e. the mouse position)
        Vector3 worldMousePos =
            cam.ScreenToWorldPoint(new Vector3(screenMousePos.x, screenMousePos.y, -cam.transform.position.z));

        int cell = Mathf.FloorToInt(worldMousePos.x);
        return cell;
    }
    
    /// <summary>
    /// Returns the rectangular area that this building would occupy, in global space.
    /// </summary>
    private RectInt GetFootprint(int cell, BuildingConstraints b)
    {
        int originX = cell - b.width / 2; // centre the building on the cursor's current cell
        return new RectInt(originX, GlobalConstants.GroundY, b.width, b.height);
    }
    
    /// <summary>
    /// Returns the world-position origin cell of this building. Origin cells are at the bottom left.
    /// </summary>
    private static Vector2 FootprintToWorld(RectInt fp) => new(fp.xMin, fp.yMin); // sprites are always alligned bottom left, so the footprint starts at the minimum (bottom left corner) of the RectInt

    private void LateUpdate()
    {
        if (!isBuildingMode) return;
        int newCell = GetCurrentCell();
        
        if (currentBuildCell != newCell)
        {
            currentBuildCell = newCell;
            hologramDirty = true; // cell position we're in changed => hologram needs re-evaluating 
        }

        if (!hologramDirty) return;
        hologramDirty = false;
        
        BuildingConstraints previous = currentBuildableSelected; // this is the building we want to be building here, if possible
        
        DetermineLegalBuildables();
        ReconcileSelection(previous);
        DrawHologram();
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
            if (IsValidBuildable(GetFootprint(currentBuildCell, b), b))
                currentBuildableHolograms.Add(b);
    }
    
    /// Keeps the previous selection if it's still legal, otherwise falls back to the first entry.
    private void ReconcileSelection(BuildingConstraints previous)
    {
        hologramSelectionIdx = currentBuildableHolograms.IndexOf(previous);
        if (hologramSelectionIdx < 0) hologramSelectionIdx = 0;
        ApplySelection();
    }
    
    /// Single place that writes currentBuildableSelected and currentFootprint from the index.
    private void ApplySelection()
    {
        currentBuildableSelected = currentBuildableHolograms.Count > 0
            ? currentBuildableHolograms[hologramSelectionIdx]
            : null;

        if (currentBuildableSelected != null)
            currentFootprint = GetFootprint(currentBuildCell, currentBuildableSelected);
    }

 
    /// <summary>
    /// Manages the cache for pre-images of unbuilt buildables and returns an unbuilt instance to show the player a preview of what this buildable would look like.
    /// </summary>
    private GameObject GetHologram(BuildingConstraints constraints)
    {
        // Check the cache, on hit return, on miss populate the cache with this building type for future use.
        if (!hologramCache.TryGetValue(constraints, out GameObject hologram))
        {
            // TODO: This does not fix the issue. We must instead write a process to capture the sprite and no other components from the prefab.
            
            hologram = Instantiate(constraints.buildingPrefab, hologramStaging);
            // *IMPORTANT* we MUST deactivate all behavioural components of this hologram, otherwise, it'll consider any instantiated but unplaced hologram a real building and cause game-breaking bugs
            foreach (Behaviour b in hologram.GetComponentsInChildren<Behaviour>(true))
            {
                b.enabled = false;
            }
            
            hologram.SetActive(false); // any newly created cached hologram can't be active or visible, that isn't our job here
            hologram.transform.SetParent(hologramRoot); 
            
            hologramCache[constraints] = hologram;
        }
        
        return hologram;
    }
    
    /// <summary>
    /// Checks all constraints on BuildingConstraints and returns None if no violation to constraints is found, or the set of all violation(s) that occurred otherwise.
    /// </summary>
    private HashSet<BuildingFailureReason> Validate(RectInt fp, BuildingConstraints b)
    {
        var failures = new HashSet<BuildingFailureReason>();
        
        if (!IsInZone(fp, b)) failures.Add(BuildingFailureReason.NotInZone);
        if (OverlapsExisting(fp)) failures.Add(BuildingFailureReason.TooCloseToAnother);
        foreach (ResourceTransaction rt in b.resourceCosts)
            if (!resources.CanConsumeResource(rt.resource, rt.amount))
                failures.Add(BuildingFailureReason.InsufficientResources);
        
        return failures;
    }

    private bool IsValidBuildable(RectInt fp, BuildingConstraints b) => Validate(fp, b).Count == 0;
    
    private bool OverlapsExisting(RectInt fp) =>
        buildingManager.IsAreaOccupiedByBuilding(fp);
    
    private bool IsInZone(RectInt fp, BuildingConstraints b)
    {
        foreach (ZoneType z in b.zonesBuildableIn)
        {
            if (zoneManager.DoesAreaFullyOverlapType(fp, z)) return true;
        }

        return false;
    }
    
    private void OnDrawGizmos()
    {
        // draw a cyan box around the footprint of the current buildable to verify that it is being drawn in the correct place
        if (!isBuildingMode || currentBuildableSelected == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(currentFootprint.center, new Vector3(currentFootprint.width, currentFootprint.height));
    }
}