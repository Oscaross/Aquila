using System;
using UnityEngine;

public class WorldState
{
    [Header("Metadata")]
    public DateTime SavedAtUtc;
    public int Version;
    
    [Header("Core Save System")]
    public int Seed;
    public readonly IdAllocator IdAllocator = new();
    
    [Header("Save Data")]
    public readonly ZoneState ZoneState = new();
    public readonly BuildingState BuildingState = new();
    // public ForestState ForestState;
}
