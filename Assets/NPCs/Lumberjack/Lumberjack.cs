using UnityEngine;

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

    private Vector2? idlePoint; // if the NPC is idling this is the random point they're currently idling to
    private Coroutine currentCallback; // the current delay callback the lumberjack is waiting on - must be cancelled and overwritten if he's assigned a job
    private int dir; // the direction the NPC is currently walking in
    private Rigidbody2D rb;
    private bool waitingToWander;
    private Transform log; // log the worker is currently dragging

    private void Awake()
    {
        SetState(LumberjackState.Idling);
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        // TODO: Make some scheduler/manager system that picks targets better
    }

    private void AssignTarget(FunctionalTree target)
    {
        Delay.Cancel(this, currentCallback); // we're likely waiting on the next idling cycle, but if we do then there's a risk that our target gets overwritten by trivial idling code instead

        currentTarget = target;
        currentCallback = null;
        waitingToWander = false;
        idlePoint = null;

        SetState(LumberjackState.WalkingTo);
    }

    private void StartChopping()
    {
        SetState(LumberjackState.Chopping);
        currentTarget.ChopDown(dir, StartDragging);
    }

    private void StartDragging()
    {
        log = currentTarget.DetachLog(); // gain reference to the transform of the log part of the felled tree
        log.SetParent(dragPoint, false);
        log.localPosition = Vector3.zero; // no offset around the lumberjack EXCEPT for the drag point

        currentTarget = null;
        SetState(LumberjackState.Dragging);
    }

    private void StoreWood()
    {
        Destroy(log.gameObject); // TODO: More elegant way of disposing of the log

        SetState(LumberjackState.Depositing);
        currentCallback = Delay.WaitThen(this, 4f, () => currentState = LumberjackState.Idling); // we want the guy to wait for a bit after dropping the logs off
    }

    private void OnIdleTargetReached()
    {
        idlePoint = null;
        waitingToWander = true;
        currentCallback = Delay.WaitThen(this, idleWaitForSeconds, ChangeIdleTarget);
    }

    private void ChangeIdleTarget()
    {
        waitingToWander = false;
        idlePoint = new Vector2(
            transform.position.x + (Random.value < 0.5f ? -1 : 1) * Random.Range(1f, 3f),
            transform.position.y);
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

    private void Update()
    {
        if (currentState == LumberjackState.Idling && currentTarget != null)
            SetState(LumberjackState.WalkingTo);
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            // Randomly walk around
            case LumberjackState.Idling:
                if (idlePoint.HasValue)
                    PathfindTo(idlePoint.Value, OnIdleTargetReached);
                else if (!waitingToWander)
                    ChangeIdleTarget();
                break;
            // Walk to the tree
            case LumberjackState.WalkingTo:
                PathfindTo(currentTarget.transform.position, StartChopping);
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
    Idling = 3,
    WalkingTo = 2,
    Chopping = 4,
    Dragging = 1,
    Depositing = 0
}

