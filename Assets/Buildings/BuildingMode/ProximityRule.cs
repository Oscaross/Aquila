using System;

[Serializable]
public class ProximityRule
{
    public ProximityMode mode;
    public BuildingConstraints target;
    public int distanceWorldUnits;
}

public enum ProximityMode
{
    Within,
    Outside
}
