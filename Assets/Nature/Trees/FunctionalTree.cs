using UnityEngine;

/**
 * Responsible for managing the lifecycle of a functional tree. E.g. sets the correct seasonal leaves, allows it to be chopped down, maintains its growth cycle from sapling => ready to harvest. 
*/

public class FunctionalTree : MonoBehaviour
{
    [SerializeField] SpriteRenderer leaves;
    [SerializeField] SpriteRenderer stump;
    [SerializeField] SpriteRenderer branches;
    [SerializeField] SpriteRenderer log;
    [SerializeField] private int currentGrowthPhase = 0;
    [SerializeField] private Sprite[] growthPhases;
    [SerializeField] private Sprite[] seasonalLeaves;
    [SerializeField] private int daysBetweenGrowthPhase = 3;
    [SerializeField] private int daysSinceLastGrew = 0;
    [SerializeField] private float fallDurationSeconds = 7f;

    /// <summary>
    /// Is a worker coming to chop down this tree?
    /// </summary>
    public bool isTargeted = false;
    /// <summary>
    /// What stage in the growth cycle is the tree at (i.e. sapling, almost grown, fully grown)
    /// </summary>
    public int CurrentGrowthPhase => currentGrowthPhase;
    /// <summary>
    /// How long the tree takes from initial chop to on the ground ready to pick up in seconds.
    /// </summary>
    public float FallDurationSeconds => fallDurationSeconds;

    private TreeFall treeFallManager;

    private void Awake()
    {
        treeFallManager = GetComponentInChildren<TreeFall>();
    }

    private void OnEnable()
    {
        TimeOfDay.OnSunrise += OnNewDay;
        TimeOfDay.OnSeasonChanged += OnSeasonChanged;
    }

    private void OnDisable()
    {
        TimeOfDay.OnSunrise -= OnNewDay;
        TimeOfDay.OnSeasonChanged -= OnSeasonChanged;
    }

    private void OnNewDay()
    {
        daysSinceLastGrew++;

        if (daysSinceLastGrew >= daysBetweenGrowthPhase) Grow();
    }

    private void OnSeasonChanged()
    {
        int seasonIdx = (int) GameTime.CurrentSeason;
        leaves.sprite = seasonalLeaves[seasonIdx];
        Debug.Log(seasonIdx);
    }

    [ContextMenu("Chop Down")]
    public void ChopDown(int chopDirection, System.Action onChopped = null)
    {
        treeFallManager.Fell(chopDirection, fallDurationSeconds);

        Delay.WaitThen(this, fallDurationSeconds + 2f, onChopped); // wait before we call back the lumberjack to drag the tree
    }

    public Transform DetachLog()
    {
        ClearTree(); // lose our branches and leaves before detaching the final log

        return log.transform;
    }

    private void ClearTree()
    {
        leaves.enabled = false;
        branches.enabled = false;
    }

    [ContextMenu("Grow")]
    private void Grow()
    {
        currentGrowthPhase = Mathf.Min(currentGrowthPhase + 1, growthPhases.Length - 1);
        // TODO: apply growthPhases[currentGrowthPhase] to the trunk renderer
        daysSinceLastGrew = 0;
    }
}
