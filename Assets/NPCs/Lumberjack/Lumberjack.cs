using UnityEngine;

/**
 * Controls the state and behaviour logic of the lumberjack NPC which is responsible for cutting down trees, dragging them back to camp and processing them.
*/

[RequireComponent(typeof(Rigidbody2D))]
public class Lumberjack : MonoBehaviour
{
    [SerializeField] private LumberjackState currentState;
    [SerializeField] private FunctionalTree currentTarget;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float pathfindingClosenessThreshold; // how close in world units we have to be before we've reached our pathfinding target
    [SerializeField] private float idleWaitForSeconds;
    [SerializeField] private Transform dragPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float chopIntervalSeconds = 0.7f; // the number of seconds we must wait between chops
    [SerializeField] private float lookForJobProbability = 0.3f;

    private Coroutine currentCallback; // the current delay callback the lumberjack is waiting on - must be cancelled and overwritten if he's assigned a job
    private int dir; // the direction the NPC is currently walking in
    private Rigidbody2D rb;
    private Transform log; // log the worker is currently dragging
    private float chopTimerSeconds; // how many secs since we last did a chop
    private LegionResources legion;
    private LumberjackWorkQueue workQueue;
    private Vector2 destination; // where are we pathfinding to
    private System.Action onArrive; // what function do we call when we reach our destination
    private Vector2 logStorePos; // where the log store (horrea) is located

    private void Awake()
    {
        SetState(LumberjackState.Idling);
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        legion = GetComponentInParent<LegionResources>();
        workQueue = GetComponentInParent<LumberjackWorkQueue>();

        logStorePos = legion.GetStore(Resource.Wood).transform.position; // TODO: Will throw null if the building isn't built yet, but idk whether lumberjacks can even exist if this building doesn't
    }

    private void Start()
    {
        ChangeIdleTarget(); // kickstart the state cycle
    }

    private void AssignTarget(FunctionalTree target)
    {
        Delay.Cancel(this, currentCallback); // we're likely waiting on the next idling cycle, but if we do then there's a risk that our target gets overwritten by trivial idling code instead

        currentTarget = target;

        target.IsTargeted = true;
        currentCallback = null;

        SetDestination(target.transform.position, StartChopping);
    }

    private void StartChopping()
    {
        if (currentState == LumberjackState.Chopping) return; // guard against re-entry, we only can start once
        // Subscribe to the tree being felled
        currentTarget.OnTreeFelled += StartDragging;
        currentTarget.OnTreeStartFalling += FinishChopping;
        SetState(LumberjackState.Chopping);
        chopTimerSeconds = chopIntervalSeconds; // we want to power up, not go straight into a chop so this needs to wait its full cooldown first
    }

    private void FinishChopping()
    {
        currentTarget.OnTreeStartFalling -= FinishChopping;
        SetState(LumberjackState.Idling); // he will simply stand at the tree until it gets felled - we probs want a state for this too eventually like StillIdle?
    }

    private void StartDragging()
    {
        log = currentTarget.DetachLog(); // gain reference to the transform of the log part of the felled tree
        log.SetParent(dragPoint, false);
        log.localPosition = Vector3.zero; // no offset around the lumberjack EXCEPT for the drag point
        log.localRotation = Quaternion.identity;

        SetState(LumberjackState.Dragging);
    }

    private void StoreWood()
    {
        Destroy(log.gameObject); // TODO: More elegant way of disposing of the log

        int yield = currentTarget.GetWoodYield();
        legion.AddResource(Resource.Wood, yield);

        currentTarget.OnTreeFelled -= StartDragging;

        currentTarget = null; // lumberjack is done with this tree

        SetState(LumberjackState.Depositing);
        currentCallback = Delay.WaitThen(this, 1f, () => ChangeIdleTarget()); // we want the guy to wait for a bit after dropping the logs off

        
    }

    private void OnIdleTargetReached()
    {
        SetState(LumberjackState.Idling);
        // Randomly look for a job once, if we get one great, go for it, otherwise continue idling.
        if (Random.value < lookForJobProbability)
        {
            FunctionalTree target;
            if (workQueue.TryGetJob(transform.position, out target))
            {
                AssignTarget(target);
                return;
            }
        }

        currentCallback = Delay.WaitThen(this, idleWaitForSeconds, ChangeIdleTarget);
    }

    private void ChangeIdleTarget()
    {
        SetState(LumberjackState.WalkingTo);
        Vector2 idlePoint = new Vector2(
            transform.position.x + (Random.value < 0.5f ? -1 : 1) * Random.Range(1f, 3f),
            transform.position.y);

        SetDestination(idlePoint, OnIdleTargetReached);
    }
    private void PathfindTo(Vector2 target, System.Action onTargetReached)
    {
        // Move the physical body to the target.
        dir = (int) Mathf.Sign(target.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

        // Rotate in direction NPC travels.
        sr.transform.localScale = new Vector3(-dir, 1f, 1f);

        float dx = Mathf.Abs(target.x - transform.position.x); // distance to target

        if (dx <= pathfindingClosenessThreshold) 
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            onTargetReached?.Invoke();
        }
    }

    private void SetState(LumberjackState next)
    {
        currentState = next;
        animator.SetInteger("State", (int) next);
    }

    private void SetDestination(Vector2 target, System.Action onArrived)
    {
        destination = target;
        onArrive = onArrived;
        SetState(LumberjackState.WalkingTo);
    }

    private void Update()
    {
        if (currentState == LumberjackState.Chopping)
        {
            chopTimerSeconds -= Time.deltaTime;
            if (chopTimerSeconds <= 0f)
            {
                chopTimerSeconds = chopIntervalSeconds;
                currentTarget.Chop(dir);
            }
        }
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            // Walk to the tree
            case LumberjackState.WalkingTo:
                PathfindTo(destination, onArrive);
                break;
            // Drag back to the camp (currently 0, 0)
            case LumberjackState.Dragging:
                PathfindTo(Vector2.zero, StoreWood);
                break;
            default:
                break;
        }
    }
}

enum LumberjackState
{
    Idling = 0,
    WalkingTo = 1,
    Chopping = 2,
    Dragging = 3,
    Depositing = 4
}