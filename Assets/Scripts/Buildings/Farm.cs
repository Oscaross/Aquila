using UnityEngine;

/**
 * Controls the state of an individual farm, including providing and calculating yield, determining when harvests occur and tracking the number of workmen. 
*/

public class Farm : MonoBehaviour
{
    [Tooltip("The expected number of days between the last harvest and the next one.")]
    [SerializeField] private int expectedDaysBetweenHarvests = 5;
    [Tooltip("Standard deviation of how much the expected harvest length can fluctuate by.")]
    [SerializeField, Range(0f, 0.3f)] private float ripenJitter = 0.12f;
    [SerializeField] private int currCycleHarvestInterval = 0; // how many days between the last harvest and the upcoming harvest
    [SerializeField] private int daysSinceLastHarvest = 0;
    private ToolDisplay toolDisplay; // visual cue updater for scythes
    private int numWorkers;

    void Awake() => toolDisplay = GetComponent<ToolDisplay>();

    public int NumWorkers
    {
        get { return numWorkers; }
        set 
        { 
            // Update the farm sprite to have the correct number of scythes.
            numWorkers = value;
            toolDisplay.SetCount(numWorkers);
        }
    }

    [ContextMenu("Add Worker")]
    public bool AddWorker()
    {
        if (numWorkers < 4)
        {
            NumWorkers = numWorkers + 1;
            return true;
        }

        return false;
    }

    [ContextMenu("Remove Worker")]
    public void RemoveWorker() => NumWorkers = Mathf.Max(0, numWorkers - 1);


    /// <summary>
    /// The proportion of the total harvest cycle that has elapsed.
    /// </summary>
    public float growthProgress => daysSinceLastHarvest / currCycleHarvestInterval;

    private void Start()
    {
        BeginNewCycle();
    }

    private void OnEnable()
    {
        TimeOfDay.OnSunrise += OnNewDay;
    }

    private void OnDisable()
    {
        TimeOfDay.OnSunrise -= OnNewDay; 
    }

    /// <summary>
    /// On the dawn of a new day this function is called. Most farm logic is done day-to-day rather than frame-by-frame.
    /// </summary>
    void OnNewDay()
    {
        daysSinceLastHarvest++;

        if (growthProgress >= 1.0f)
        {
            Harvest();
        }
    }

    /// <summary>
    /// After a harvest, reconfigure the farm ready to determine the number of days until the next harvest.
    /// </summary>
    void BeginNewCycle()
    {
        // Add some randomness to the number of days until next harvest by scaling the expected harvest interval by some randomised factor.
        float mult = Mathf.Clamp(Probability.SampleGaussian(1f, ripenJitter), 0.75f, 1.25f); // a multiplier picked from a normal distribution over a mean of 1x and standard eviation of ripenJitter, bounded between 75% (min) and 125% (max)
        currCycleHarvestInterval = Mathf.RoundToInt(expectedDaysBetweenHarvests * mult);
        daysSinceLastHarvest = 0;
    }

    void Harvest()
    {
        int yield = 100;

        Debug.Log($"Harvesting now! Yield: {yield}");
        BeginNewCycle();
    }
}
