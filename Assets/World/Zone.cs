using UnityEngine;

[System.Serializable]
public struct Zone
{
    public ZoneType type;
    public RectInt bounds;

    public Zone(RectInt bounds, ZoneType type)
    {
        this.bounds = bounds;
        this.type = type;
    }
}
