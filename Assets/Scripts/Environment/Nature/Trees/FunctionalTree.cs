using UnityEngine;

/**
 * Responsible for managing the lifecycle of a functional tree. E.g. sets the correct seasonal leaves, allows it to be chopped down, maintains its growth cycle from sapling => ready to harvest. 
*/

public class FunctionalTree : MonoBehaviour
{
    [SerializeField] SpriteRenderer leaves;
    [SerializeField] SpriteRenderer stump;
    [SerializeField] SpriteRenderer branches;
    [SerializeField] private int currentGrowthPhase = 0;
    [SerializeField] private Sprite[] growthPhases;
    [SerializeField] private Sprite[] seasonalLeaves;
    [SerializeField] private int daysBetweenGrowthPhase = 3;
    [SerializeField] private int daysSinceLastGrew = 0;

    public bool isTargeted = false;
    public int CurrentGrowthPhase => currentGrowthPhase;

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
    public void ChopDown()
    {
        int direction = Random.value < 0.5f ? -1 : 1;
        treeFallManager.Fell(direction);

        Delay.WaitThen(this, 7f, ClearTree);
    }

    private void ClearTree()
    {
        Debug.Log("Clearing tree");
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
