using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    private readonly List<Building> buildings = new();
    private readonly List<SubjectWorkspace> buildingsWithWorkspaces = new();
    public List<Building> Buildings => buildings;

    private Legion legion;

    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
    }

    public bool IsAreaOccupiedByBuilding(RectInt rectWorldSpace)
    {
        foreach (Building b in buildings)
        {
            if (b.FootprintInWorldSpace.Overlaps(rectWorldSpace)) return true;
        }

        return false;
    }

    public void Register(Building b)
    {
        buildings.Add(b);
        
        if (b.TryGetComponent(out SubjectWorkspace w))
        {
            buildingsWithWorkspaces.Add(w); // if the building is also a workspace then track the workspace separately and manage it
            // this is a job building so it must be added to the job board
            legion.SubjectManager.RegisterJob(w);
        }
            
        b.transform.SetParent(transform);
    }

    private void FixedUpdate()
    {
        foreach (var workspace in buildingsWithWorkspaces)
        {
            workspace.Tick(Time.deltaTime);
        }
    }
}
