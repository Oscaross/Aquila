using System;

public class WorldState
{
    public DateTime SavedAtUtc; 
    public IdAllocator IdAllocator;
    public int Version;
    public ZoneState Zones;
}
