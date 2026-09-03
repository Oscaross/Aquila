using UnityEngine;
using System;
using NUnit.Framework;
using Unity.VisualScripting;

/**
 * Responsible for maintaining the correct GameTime and firing events when certain time-related milestones are reached. 
*/
public class TimeOfDay : MonoBehaviour
{
    public static TimeOfDay Instance;
    
    [Header("Day-Night Cycle")]
    [SerializeField] private float dayLengthSeconds = 600; // one day = 10 IRL minutes
    [SerializeField] private float timeMultiplier = 1.0f;
    [SerializeField] private float timeNow = 0.25f; // start at sunrise

    [Header("Annual Cycle")]
    [SerializeField] private int daysInASeason = 18;
    [SerializeField] private Season startSeason = Season.Spring;

    /// <summary>
    ///  A float over [0, 1] that defines the global time of day. 0.0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset and 1.0 wraps around to 0.0 to restart the cycle.
    /// </summary>
    [UnityEngine.Range(0, 1)]
    public float TimeNow => timeNow;
    public int DayCount { get; private set; }
    public static event Action OnSunset;
    public static event Action OnSunrise;
    public static event Action OnNoon;
    public static event Action OnSeasonChanged;

    /// <summary>
    /// The conversion between realtime seconds and in-game seconds. e.g. TimeMultiplier = 1.0 means every 1 second in real life is 1 second in the game, = 2.0 means every 1 second in real life is 0.5 seconds in game etc...
    /// </summary>
    public float TimeMultiplier => timeMultiplier;

    private bool didSunriseHappen = false;
    private bool didSunsetHappen = false;
    private bool didNoonHappen = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        GameTime.CurrentSeason = startSeason;
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        Debug.Assert(dayLengthSeconds > 0);

        timeNow += (Time.deltaTime * timeMultiplier) / dayLengthSeconds; // increment the time float by the fraction of time out of the day that has elapsed since last frame, scaled by the multiplier
       
        // Wrap timeNow around to 1.0f to prevent times over 1.
        if (timeNow >= 1.0f)
        {
            timeNow -= 1.0f;
            didSunsetHappen = false;
            didSunriseHappen = false;
            didNoonHappen = false;
        }

        if (GameTime.IsNoon && !didNoonHappen)
        {
            OnNoon?.Invoke();
            didNoonHappen = true;
        }

        // Sunset?
        if (GameTime.IsSunset && !didSunsetHappen)
        {
            Sunset();
        }

        // Sunrise?
        if (GameTime.IsSunrise && !didSunriseHappen)
        {
            Sunrise();
        }

        GameTime.Now = timeNow;
    }

    /// <summary>
    /// Handles sunset logic.
    /// </summary>
    void Sunset()
    {
        OnSunset?.Invoke();
        didSunsetHappen = true;
    }

    /// <summary>
    /// Handles sunrise logic. Note that a sunrise is considered the start of a new day, so this is where most "new day" code is called from.
    /// </summary>
    void Sunrise()
    {
        OnSunrise?.Invoke();
        DayCount++;
        didSunriseHappen = true;

        CheckSeason();
    }

    /// <summary>
    /// Handles logic relating to seasons when a new day begins.
    /// </summary>
    void CheckSeason()
    {
        // Advance the season forward if daysInASeason days have elapsed.
        if (DayCount % daysInASeason == 0)
        {
            int seasonIdx = (int) GameTime.CurrentSeason + 1; // casts to indexed season (Spring = 1, Summer = 2...)
            GameTime.CurrentSeason = (Season) (seasonIdx % 4);
            OnSeasonChanged?.Invoke();
        }
    }
}
