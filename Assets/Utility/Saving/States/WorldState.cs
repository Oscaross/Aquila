using System;
using UnityEngine;

public class WorldState
{
    [Header("Metadata")]
    public DateTime SavedAtUtc;
    public int Version;
    
    [Header("Core Save System")]
    public int Seed;
    public IdAllocator IdAllocator = new();
    
    [Header("Save Data")]
    public ZoneState ZoneState = new();
    // public ForestState ForestState;
}
