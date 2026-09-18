using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    private List<Building> buildings = new();
    
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
    }
}
