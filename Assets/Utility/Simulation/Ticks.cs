using UnityEngine;

public class Ticks
{
    /// <summary>
    /// How many "ticks" (calls of FixedUpdate) occur every second. Changing this value will break old save timers. A save file encodes elapsed
    /// </summary>
    public const int PerSecond = 50;

    public static int FromSeconds(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * PerSecond));
    public float ToSeconds(long ticks) => ticks / (float)PerSecond;
}
