using UnityEngine;

/**
 * Responsible for maintaining the correct GameTime and firing events when certain time-related milestones are reached. 
*/
public class TimeOfDay : MonoBehaviour
{
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
    [Range(0, 1)]
    public float TimeNow => timeNow;
    public int DayCount { get; private set; }
    public static event System.Action OnSunset;
    public static event System.Action OnSunrise;

    private bool didSunriseHappen = false;
    private bool didSunsetHappen = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        GameTime.CurrentSeason = startSeason;
    }

    void Update()
    {
        if (dayLengthSeconds <= 0f) Debug.LogError("A day cannot have a negative/zero number of seconds!");

        timeNow += (Time.deltaTime * timeMultiplier) / dayLengthSeconds; // increment the time float by the fraction of time out of the day that has elapsed since last frame, scaled by the multiplier
       
        // Wrap timeNow around to 1.0f to prevent times over 1.
        if (timeNow >= 1.0f)
        {
            timeNow -= 1.0f;
            didSunsetHappen = false;
            didSunriseHappen = false;
        }

        // Sunset?
        if (timeNow >= 0.72f && timeNow <= 0.73f && !didSunsetHappen)
        {
            Sunset();
        }

        // Sunrise?
        if (timeNow <= 0.21f && timeNow >= 0.2f && !didSunriseHappen)
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
        }
    }
}
