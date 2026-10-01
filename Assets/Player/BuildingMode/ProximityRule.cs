using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProximityRule
{
    [Tooltip("Usually either we want a rule to say WITHIN x units or OUTSIDE x units of target.")]
    public ProximityMode mode;
    [Tooltip("The type(s) of buildings that this rule should apply to.")]
    public List<BuildingType> buildingsThisAppliesTo;
    [Tooltip("If within then this building must be within x units of some building in the buildings this applies to set.")]
    public int distanceWorldUnits;

    public bool IsSatisfiedBy(RectInt candidate, IReadOnlyList<Building> buildingsPlaced)
    {
        switch (mode)
        {
            case ProximityMode.Outside:

                if (buildingsThisAppliesTo == null) return true;
                
                // Loop over each building, making sure dx is no less than distanceWorldUnits for any buildings this applies to
                foreach (var building in buildingsPlaced)
                {
                    if (!buildingsThisAppliesTo.Contains(building.Constraints)) continue; // we don't care about the constraint if the constraint doesn't apply to this building
                    int dx = MinDistanceOrZeroIfOverlap(candidate, building.FootprintInWorldSpace);
                    // The building is on this constraint and we're less than the min distance to it => constraint violated
                    if (dx < distanceWorldUnits) return false; 
                }

                return true; // no return in the loop implies no constraint violated
            case ProximityMode.Within:

                if (buildingsThisAppliesTo == null) return false;
                
                foreach (var building in buildingsPlaced)
                {
                    if (!buildingsThisAppliesTo.Contains(building.Constraints)) continue;
                    
                    int dx = MinDistanceOrZeroIfOverlap(candidate, building.FootprintInWorldSpace);
                    // If this building is within distanceWorldUnits from some building in our constraint, the constraint is satisfied.
                    // This is an exists quantifier, not a for all. We only care if a singular building exists that makes the constraint true.
                    if (dx <= distanceWorldUnits) return true;
                }
                
                return false; // no building was found, none exists, constraint violated.
            default:
                Debug.LogError("ProximityConstraint exists that is not a Within or Outside constraint.");
                return false;
        }
    }
    
    // The distance between our candidate building and the building we're currently checking it against in the world
    // Returns 0 for touching or overlapping buildings, otherwise the empty cell gap.
    private int MinDistanceOrZeroIfOverlap(RectInt other, RectInt candidate) => Math.Max(0, Math.Max(other.xMin - candidate.xMax, candidate.xMin - other.xMax));
}

public enum ProximityMode
{
    Within,
    Outside
}
