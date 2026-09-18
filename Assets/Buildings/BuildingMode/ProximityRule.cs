using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProximityRule
{
    [Tooltip("Usually either we want a rule to say WITHIN x units or OUTSIDE x units of target.")]
    public ProximityMode mode;
    [Tooltip("The type(s) of buildings that this rule should apply to.")]
    public HashSet<BuildingConstraints> buildingsThisAppliesTo;
    [Tooltip("If within then this building must be within x units of some building in the buildings this applies to set.")]
    public int distanceWorldUnits;
    [Tooltip("When checking if a building is close enough to this one, allow buildings that directly overlap this building (i.e. you might want a field in front of a farm).")]
    public bool allowDirectOverlaps;
}

public enum ProximityMode
{
    Within,
    Outside
}
