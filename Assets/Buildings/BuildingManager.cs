using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : GameManagerComponent
{
    private readonly List<Building> buildings = new();
    private readonly List<SubjectWorkspace> buildingsWithWorkspaces = new();
    public IReadOnlyList<Building> Buildings => buildings;

    private Legion legion;
    private WorldState save;
    private WorldContext saveContext;

    public override void Restore(WorldState state, WorldContext context)
    {
        save = state;
        saveContext = context;

        foreach (var b in state.BuildingState.Buildings.Items)
        {
            InstantiateBuilding(b);
        }
    }

    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
    }

    // Create the in-game instance of the building, deliver it to job boards so that NPCs can react to it and write it to the save file.
    private void InstantiateBuilding(BuildingModel data)
    {
        Building b = Instantiate(data.Constraints.buildingPrefab, new Vector3(data.XMin, data.YMin, 0), Quaternion.identity,
            transform).GetComponent<Building>();
        
        buildings.Add(b);
        
        if (b.TryGetComponent(out SubjectWorkspace w))
        {
            buildingsWithWorkspaces.Add(w); // if the building is also a workspace then track the workspace separately and manage it
            // this is a job building so it must be added to the job board
            legion.SubjectManager.RegisterJob(w);
        }
        
        b.Bind(data);
        saveContext.Registry.Register(b);
    }

    public bool IsAreaOccupiedByBuilding(RectInt rectWorldSpace)
    {
        foreach (Building b in buildings)
        {
            if (b.FootprintInWorldSpace.Overlaps(rectWorldSpace)) return true;
        }

        return false;
    }

    /// <summary>
    /// Build a new building.
    /// </summary>
    /// <param name="origin">The bottom left of the building's integer position in world coordinates.</param>
    /// <param name="type">The type of building to be built.</param>
    public void CreateNewBuilding(Vector2Int origin, BuildingType type)
    {
        var model = new BuildingModel(type.Key, origin.x, origin.y);
        save.BuildingState.Buildings.Add(model, save.IdAllocator);
        InstantiateBuilding(model);
    }

    private void FixedUpdate()
    {
        foreach (var workspace in buildingsWithWorkspaces)
        {
            workspace.Tick(Time.deltaTime);
        }
    }
}
