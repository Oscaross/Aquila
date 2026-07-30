using UnityEngine;

/*
 * Responsible for containing and ordering the various multipliers and variables that change throughout the year's season cycle.
*/

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

[System.Serializable]
public class SeasonData
{
    [Header("Agriculture")]
    [Tooltip("The multiplier for how much grain a farm produces in the given season.")]
    public float yieldMultiplier;
    [Tooltip("The mean number of days between harvests at a given farm in the given season.")]
    public int expectedHarvestIntervalDays;
}
