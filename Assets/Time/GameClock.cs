using System;
using UnityEngine;

/// <summary>
/// The single source of game time. Advances the clock, exposes it statically, and fires time events.
/// Now: 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.
/// </summary>
public class GameClock : MonoBehaviour
{
    [Header("Day-Night Cycle")]
    [SerializeField, Min(1f)] private float dayLengthSeconds = 600f;
    [Tooltip("Game seconds per real second. 2 = time runs twice as fast.")]
    [SerializeField, Min(0f)] private float timeMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float startTime = 0.25f;

    [Header("Annual Cycle")]
    [SerializeField, Min(1)] private int daysInASeason = 18;
    [SerializeField] private Season startSeason = Season.Spring;
    
    [Header("Debug")]
    [Tooltip("Drag to scrub time. Shows the live time while playing.")]
    [SerializeField, Range(0f, 1f)] private float scrubTime = 0.25f;

    private const float SunriseStart = 0.21f, SunriseEnd = 0.28f;
    private const float SunsetStart = 0.72f, SunsetEnd = 0.79f;
    private const float NoonMark = 0.5f;

    public static float Now { get; private set; }
    /// <summary>Game seconds elapsed while unpaused. Never wraps; use for animation.</summary>
    public static float ElapsedSeconds { get; private set; }
    public static int Day { get; private set; }
    public static Season CurrentSeason { get; private set; }
    public static bool IsPaused { get; set; }
    public static float TimeMultiplier => instance ? instance.timeMultiplier : 1f;

    public static bool IsNight => Now < 0.25f || Now > 0.75f;
    public static bool IsSunrise => Now > SunriseStart && Now < SunriseEnd;
    public static bool IsSunset => Now > SunsetStart && Now < SunsetEnd;

    public static event Action OnSunrise, OnNoon, OnSunset, OnSeasonChanged;

    private static GameClock instance;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        Now = startTime;
        scrubTime = Now;
        ElapsedSeconds = 0f;
        Day = 0;
        CurrentSeason = startSeason;
        IsPaused = false;
    }

    private void Update()
    {
        float prev = 0f;
        
        if (scrubTime != Now)
        {
            prev = Now;
            Now = scrubTime;
        }
        
        if (!IsPaused)
        {
            float dt = Time.deltaTime * timeMultiplier;
            prev = Now;

            ElapsedSeconds += dt;
            Now = Mathf.Repeat(Now + dt / dayLengthSeconds, 1f);
        }

        scrubTime = Now;
        if (Crossed(prev, Now, SunriseStart)) Sunrise();
        if (Crossed(prev, Now, NoonMark)) OnNoon?.Invoke();
        if (Crossed(prev, Now, SunsetStart)) OnSunset?.Invoke();
    }

    /// <summary>True if time moved past mark this frame, including across the midnight wrap.</summary>
    private static bool Crossed(float prev, float now, float mark) =>
        now >= prev ? prev < mark && now >= mark
                    : prev < mark || now >= mark;

    private void Sunrise()
    {
        Day++;
        OnSunrise?.Invoke();

        if (Day % daysInASeason == 0)
        {
            CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);
            OnSeasonChanged?.Invoke();
        }
    }

    public static bool TryGetSunriseProgress(float t, out float p) => TryProgress(t, SunriseStart, SunriseEnd, out p);
    public static bool TryGetSunsetProgress(float t, out float p) => TryProgress(t, SunsetStart, SunsetEnd, out p);

    private static bool TryProgress(float t, float start, float end, out float p)
    {
        p = 0f;
        if (t < start || t > end) return false;
        p = (t - start) / (end - start);
        return true;
    }

    /// <summary>
    /// Fetches the current time with some offset, clamped to the [0, 1] interval of the day night cycle.
    /// </summary>
    /// <param name="offset">The offset, in proportional time terms. An offset of 0.5 will set the time to be "a half of the day night cycle ahead" and -0.5 will be "a full half of the day night cycle behind."</param>
    /// <returns></returns>
    public static float NowOffsetBy(float offset) => Mathf.Repeat(Now + offset, 1f);
    
    /// <summary>0 at noon, rising to 1 at the next noon. Never wraps mid-night.</summary>
    public static float SinceNoon => Mathf.Repeat(Now - 0.5f, 1f);
}