using UnityEngine;

/**
 * The endpoint for all GameObjects to query the current time. This class only contains time data and displays it to downstream consumers. The class has no responsibility for providing time to the correct classes or updating the time value. 
*/
public static class GameTime
{
    /// <summary>
    /// A float over [0, 1] that defines the current time. 0 = midnight, 0.25 = sunrise, 0.5 = midday, 0.75 = sunset and 1.0 wraps back around to midnight.
    /// </summary>
    public static float Now { get; internal set; }
    /// <summary>
    /// The number of days that have elapsed since the player began this save. A day is the time taken for the Now variable to move from 0.0 back around to 0.0 (one full day-night cycle).
    /// </summary>
    public static int Day { get; internal set; }
    public static Season CurrentSeason { get; internal set; }
    public static bool IsNight => Now < 0.25f || Now > 0.75f;
    public static bool IsSunrise => Now > 0.22f && Now < 0.28f;
    public static bool IsSunset => Now > 0.72f && Now < 0.78f;
}