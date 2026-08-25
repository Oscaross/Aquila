using UnityEngine;

/**
 * Responsible for managing the lifecycle of a functional tree. E.g. sets the correct seasonal leaves, allows it to be chopped down, maintains its growth cycle from sapling => ready to harvest. 
*/

public class FunctionalTree : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] SpriteRenderer sapling; // the sapling is a single sprite and is removed once the tree becomes fully grown
    [SerializeField] SpriteRenderer leaves; // leaves get changed by season/removed in winter
    [SerializeField] SpriteRenderer stump; // stumps remain after a tree is chopped
    [SerializeField] SpriteRenderer branches; // branches are removed when the tree falls
    [SerializeField] SpriteRenderer log; // logs are dragged back by lumberjacks after felling

    [Header("Logic")]
    [SerializeField] private int currentGrowthPhase = 0; 
    [SerializeField] private Sprite[] growthPhases; // the various sapling sprites for this tree
    [SerializeField] private int daysBetweenGrowthPhase = 3; // how long it takes to go to the next sapling sprite
    [SerializeField] private int daysSinceLastGrew = 0; 
    [SerializeField] private float fallDurationSeconds = 3f;
    [SerializeField] private int hardness = 5; // how many hits of an axe it takes for the tree to fall
    [SerializeField] private int waitAfterFellSeconds = 2; // how long in secs we wait after the tree is felled before emitting the on felled signal
    [SerializeField] private int baseWoodYield = 50;
    [SerializeField] private int daysUntilStumpDisappears = 2;

    public event System.Action OnTreeFelled; // called after the tree settles and has hit the ground
    public event System.Action OnTreeStartFalling; // called immediately when the tree begins its fell animation
    public event System.Action<FunctionalTree> OnTreeExpired; // called to destroy the tree after it has been felled and its stump is expired

    /// <summary>
    /// Is a worker coming to chop down this tree?
    /// </summary>
    public bool IsTargeted = false;
    /// <summary>
    /// Has this tree been chopped down?
    /// </summary>
    public bool IsFelled => isFelled;
    private bool isFelled = false;
    /// <summary>
    /// What stage in the growth cycle is the tree at (i.e. sapling, almost grown, fully grown)
    /// </summary>
    public int CurrentGrowthPhase => currentGrowthPhase;
    /// <summary>
    /// How long the tree takes from initial chop to on the ground ready to pick up in seconds.
    /// </summary>
    public float FallDurationSeconds => fallDurationSeconds;
    /// <summary>
    /// Could a lumberjack legally chop this tree?
    /// </summary>
    public bool CanTarget => isGrown && !IsFelled && !IsTargeted;

    private bool isGrown = false;

    private TreeFall treeFallManager;
    private Shaker shaker;
    private int treeHealth; // how many chops has this particular tree had
    private int daysSinceFelled;

    private void Awake()
    {
        treeFallManager = GetComponentInChildren<TreeFall>();
        shaker = GetComponentInChildren<Shaker>();
        treeHealth = hardness; // the tree starts with its total hardness as its health

        // Make sure that trees display either saplings or their fully grown sprite on spawn according to their growth phase
        isGrown = currentGrowthPhase >= growthPhases.Length - 1;
        ResolveCurrentGrowthPhase();
    }

    private void OnEnable()
    {
        TimeOfDay.OnSunrise += OnNewDay;
    }

    private void OnDisable()
    {
        TimeOfDay.OnSunrise -= OnNewDay;
    }

    private void OnNewDay()
    {
        if (daysSinceFelled >= daysUntilStumpDisappears) Expire();
        if (isFelled)
        {
            daysSinceFelled++;
            return;
        }

        daysSinceLastGrew++;

        if (daysSinceLastGrew >= daysBetweenGrowthPhase) Grow();
    }

    public void Chop(Direction chopDirection)
    {
        treeHealth--;
        shaker.Shake();

        if (treeHealth <= 0)
        {
            Fell(chopDirection);
            OnTreeStartFalling?.Invoke();
        }
    }

    /// <summary>
    /// Returns the amount of wood that this tree produced.
    /// </summary>
    /// <returns>The amount of wood this tree produced.</returns>
    public int GetWoodYield()
    {
        return baseWoodYield;
    }

    private void Fell(Direction chopDirection)
    {
        treeFallManager.Fell(chopDirection, fallDurationSeconds);
        isFelled = true;
        daysSinceFelled = 0;
        Delay.WaitThen(this, fallDurationSeconds + waitAfterFellSeconds, () => OnTreeFelled?.Invoke());
    }

    public Transform DetachLog()
    {
        ClearTree(); // lose our branches and leaves before detaching the final log

        log.transform.SetParent(null, true); // no parent so lumberjack can own the log instead - this stops rotation bugs and also means we can safely destroy this tree anytime
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
        if (isGrown) return;

        currentGrowthPhase++;
        daysSinceLastGrew = 0;
        isGrown = currentGrowthPhase >= growthPhases.Length - 1;

        ResolveCurrentGrowthPhase();
    }

    private void Expire()
    {
        OnTreeExpired.Invoke(this);
    }

    private void ResolveCurrentGrowthPhase()
    {
        sapling.enabled = !isGrown;
        leaves.enabled = isGrown;
        stump.enabled = isGrown;
        branches.enabled = isGrown;
        log.enabled = isGrown;

        if (!isGrown)
        {
            sapling.sprite = growthPhases[currentGrowthPhase];
        }
    }
}
