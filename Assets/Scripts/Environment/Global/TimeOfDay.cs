using UnityEngine;

public class TimeOfDay : MonoBehaviour
{
    [SerializeField] private float dayLengthSeconds = 600; // one day = 10 IRL minutes
    [SerializeField] private float timeMultiplier = 1.0f;
    [SerializeField] private float timeNow = 0.25f; // start at sunrise
    /// <summary>
    ///  A float over [0, 1] that defines the global time of day. 0.0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset and 1.0 wraps around to 0.0 to restart the cycle.
    /// </summary>
    [Range(0, 1)]
    public float TimeNow => timeNow;
    public int DayCount { get; private set; }

    public event System.Action<int> OnNewDay;
    public static event System.Action OnSunset;
    public static event System.Action OnSunrise;

    private bool didSunriseHappen = false;
    private bool didSunsetHappen = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        Debug.Log("My name is Gabriel and I have a smelly bum bum.");
    }

    void Update()
    {
        if (dayLengthSeconds <= 0f) Debug.LogError("A day cannot have a negative/zero number of seconds!");

        timeNow += (Time.deltaTime * timeMultiplier) / dayLengthSeconds; // increment the time float by the fraction of time out of the day that has elapsed since last frame, scaled by the multiplier
        
        // Check progression to the next day.
        if (timeNow >= 1.0f)
        {
            timeNow -= 1.0f;
            DayCount++;
            didSunsetHappen = false;
            didSunriseHappen = false;
            OnNewDay?.Invoke(DayCount); // incremented the day count by one and notify subscriber methods that a new day is here
        }

        if (timeNow >= 0.72f && timeNow <= 0.73f && !didSunsetHappen)
        {
            OnSunset?.Invoke();
            didSunsetHappen = true;
        }

        if (timeNow <= 0.21f && timeNow >= 0.2f && !didSunriseHappen)
        {
            OnSunrise?.Invoke();
            didSunriseHappen = true;
        }
    }
}
