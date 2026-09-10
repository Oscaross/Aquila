using UnityEngine;

[System.Serializable]
public struct Zone : System.IEquatable<Zone>
{
    public RectInt bounds;
    public ZoneType type;

    public Zone(RectInt bounds, ZoneType type)
    {
        this.bounds = bounds;
        this.type = type;
    }

    public bool Equals(Zone other) => bounds.Equals(other.bounds) && type == other.type;

    public override bool Equals(object obj) => obj is Zone other && Equals(other);

    public override int GetHashCode() => System.HashCode.Combine(bounds, type);

    public static bool operator ==(Zone a, Zone b) => a.Equals(b);
    public static bool operator !=(Zone a, Zone b) => !a.Equals(b);
}
