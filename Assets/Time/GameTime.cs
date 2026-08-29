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
    public static bool IsSunrise => Now > SunriseStart && Now < SunriseEnd;
    public static bool IsSunset => Now > SunsetStart && Now < SunsetEnd;
    public static bool IsNoon => Now > 0.47f && Now < 0.53f;
    
    private const float SunriseStart = 0.21f;
    private const float SunriseEnd = 0.28f;
    private const float SunsetStart = 0.72f;
    private const float SunsetEnd = 0.79f;

    /// <summary>
    /// Calculates what proportion of the sunrise has been completed. i.e. if it starts at t = 0.1 and ends at t = 0.2 and the time is t = 0.15 this returns 0.5
    /// </summary>
    /// <param name="forTime">The current time.</param>
    /// <param name="progress">Variable to write the result to.</param>
    /// <returns>True if it is sunrise, false if this was called but it is not currently sunrise.</returns>
    public static bool TryGetSunriseProgress(float forTime, out float progress)
    {
        progress = 0f;
        if (forTime < SunriseStart || forTime > SunriseEnd) return false;
        progress = (forTime - SunriseStart) / (SunriseEnd - SunriseStart);
        return true;
    }

    /// <summary>
    /// Calculates what proportion of the sunset has been completed. i.e. if it starts at t = 0.1 and ends at t = 0.2 and the time is t = 0.15 this returns 0.5
    /// </summary>
    /// <param name="forTime">The current time.</param>
    /// <param name="progress">Variable to write the result to.</param>
    /// <returns>True if it is sunset, false if this was called but it is not currently sunset.</returns>
    public static bool TryGetSunsetProgress(float forTime, out float progress)
    {
        progress = 0f;
        if (forTime < SunsetStart || forTime > SunsetEnd) return false;
        progress = (forTime - SunsetStart) / (SunsetEnd - SunsetStart);
        return true;
    }
}