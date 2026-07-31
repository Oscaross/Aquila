using UnityEngine;

/**
 * Controls the state of an individual farm, including providing and calculating yield, determining when harvests occur and tracking the number of workmen. 
*/

public class Farm : MonoBehaviour
{
    [Tooltip("Standard deviation of how much the expected harvest length can fluctuate by.")]
    [SerializeField, Range(0f, 0.3f)] private float ripenJitter = 0.12f;
    [Tooltip("How many days after the previous harvest does the current harvest occur.")]
    [SerializeField] private int currCycleHarvestInterval = 0;
    [Tooltip("Number of days elapsed since last harvest.")]
    [SerializeField] private int daysSinceLastHarvest = 0;
    [Tooltip("The amount of grain a farm with no multipliers and one worker would produce.")]
    [SerializeField] private int baseFarmYield = 100;
    [Tooltip("How much of the yield from a farm with one worker does each additional worker add.")]
    [SerializeField] private float[] workerMultipliers;
    [Tooltip("A farm is deserted if there are no workers tending to it (for example it has no workers assigned or it is pillaged). Deserted farms do not grow or produce any yield regardless of season.")]
    [SerializeField] private bool isDeserted = false;

    [Tooltip("Each growth phase sprite for the farm background.")]
    [SerializeField] private Sprite[] growthPhaseBackgrounds;
    [SerializeField] private SpriteRenderer currentBackground;

    private ToolDisplay toolDisplay; // visual cue updater for scythes
    private int numWorkers;
    private SeasonTable seasonTable; // data tables for the given season
    private LegionResources legion; // resource tracker

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
            if (isDeserted) isDeserted = false;

            NumWorkers = numWorkers + 1;
            return true;
        }

        return false;
    }

    [ContextMenu("Remove Worker")]
    public void RemoveWorker() => NumWorkers = Mathf.Max(0, numWorkers - 1);

    private void RefreshGrowthSprite()
    {
        if (growthPhaseBackgrounds.Length == 0 || currentBackground == null) return;
        int idx = Mathf.Clamp(
            Mathf.FloorToInt(growthProgress * growthPhaseBackgrounds.Length),
            0, growthPhaseBackgrounds.Length - 1);
        currentBackground.sprite = growthPhaseBackgrounds[idx];
    }

    /// <summary>
    /// The proportion of the total harvest cycle that has elapsed.
    /// </summary>
    public float growthProgress => (currCycleHarvestInterval != 0) ? (float) daysSinceLastHarvest / currCycleHarvestInterval : 0; // avoid NaN

    private void Start()
    {
        BeginNewCycle();
        RefreshGrowthSprite();
    }

    private void Awake()
    {
        toolDisplay = GetComponent<ToolDisplay>();
        seasonTable = SeasonTable.Instance;
        legion = GetComponentInParent<LegionResources>();
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
        if(isDeserted) return;
        if (numWorkers == 0)
        {
            DesertFarm();
        }

        daysSinceLastHarvest++;

        if (growthProgress >= 1.0f && GameTime.CurrentSeason != Season.Winter)
        {
            Harvest();
        }

        // Update crop background state.
        RefreshGrowthSprite();
    }

    /// <summary>
    /// After a harvest, reconfigure the farm ready to determine the number of days until the next harvest.
    /// </summary>
    void BeginNewCycle()
    {
        // Add some randomness to the number of days until next harvest by scaling the expected harvest interval by some randomised factor.
        float mult = Mathf.Clamp(Probability.SampleGaussian(1f, ripenJitter), 0.75f, 1.25f); // a multiplier picked from a normal distribution over a mean of 1x and standard eviation of ripenJitter, bounded between 75% (min) and 125% (max)
        currCycleHarvestInterval = Mathf.RoundToInt(seasonTable.GetCurrentSeasonData().expectedHarvestIntervalDays * mult);
        daysSinceLastHarvest = 0;
    }

    void Harvest()
    {
        float yield = 0;
        
        for (int i = 0; i < numWorkers; i++)
        {
            float workerMult = workerMultipliers[i];
            yield += workerMult * baseFarmYield;
        }

        yield *= SeasonTable.Instance.GetCurrentSeasonData().yieldMultiplier;

        // Use a Gaussian to add variance. Mean is the calculated yield and standard deviation is 10% of that calculated yield.

        yield = Probability.SampleGaussian(yield, yield * 0.1f);
        int yieldRounded = (int) Mathf.Ceil(yield);

        legion.AddResource(Resource.Grain, yieldRounded);
        BeginNewCycle();
    }

    /// <summary>
    /// Removes crop progress and relevant sprites. Use if the farm is for example deserted or pillaged.
    /// </summary>
    void DesertFarm()
    {
        isDeserted = true;
        BeginNewCycle();
    }
}
