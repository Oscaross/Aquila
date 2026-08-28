using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Slinger : MonoBehaviour
{
    [SerializeField] private SlingerState currentState;
    // [SerializeField] private Animator animator;
    [Tooltip("Time elapsed between the slinger beginning the slingshot reload and starting the firing animation in seconds.")]
    [SerializeField] private float loadingTimeSeconds;
    [Tooltip("Time elapsed between the slinger starting the firing animation and the projectile being released onto its trajectory in seconds.")]
    [SerializeField] private float firingTimeSeconds;
    [Tooltip("Time elapsed between slinger finishing firing and starting reloading for the next shot.")]
    [SerializeField] private float betweenShotsTimeSeconds;
    [Tooltip("The distance in world units that the slinger should stand away from its actual target.")]
    [SerializeField] private float targetStandingRange;

    private Pathfinder pathfinder;
    private RangedAttacker rangedAttacker;
    private Animator animator;
    private Legion legion;
    private Vector2 currentTarget;

    private void Awake()
    {
        pathfinder = GetComponent<Pathfinder>();
        rangedAttacker = GetComponent<RangedAttacker>();
        legion = GetComponentInParent<Legion>();
        animator = GetComponentInChildren<Animator>();
    }

    private void SetState(SlingerState next)
    {
        currentState = next;
        animator.SetInteger("State", (int) next);
    }

    public void SetTarget(Vector2 target)
    {
        SetState(SlingerState.WalkingTo);
        currentTarget = target;

        int sign = DirectionExtensions.FromDelta(target.x - transform.position.x).Sign();
        Vector2 standingPoint = new Vector2(target.x - sign * targetStandingRange, 0f); // where we'll stand (some dx away from the target) and start firing

        pathfinder.PathfindTo(standingPoint, LoadSlingshot);
    }

    private void LoadSlingshot()
    {
        SetState(SlingerState.Reloading);
        pathfinder.SetFacing(DirectionExtensions.FromDelta(currentTarget.x - transform.position.x)); // face the target

        Delay.WaitThen(this, loadingTimeSeconds, FireSlingshot);
    }

    private void FireSlingshot()
    {
        SetState(SlingerState.Firing);
        
        Delay.WaitThen(this, firingTimeSeconds, () =>
        {
            SetState(SlingerState.Idling);
            if(!rangedAttacker.TryFireAt(currentTarget, OnTargetHit, OnTargetMiss)) Debug.Log("I should reposition");
            Delay.WaitThen(this, betweenShotsTimeSeconds, LoadSlingshot);
        });
    }

    private void OnTargetMiss()
    {
        Debug.Log("[Slinger] Oh no! We missed.");
    }

    private void OnTargetHit()
    {
        Debug.Log("[Slinger] Oh yo! We hit.");
    }
}

enum SlingerState
{
    Spawning = 0,
    WalkingTo = 1,
    Reloading = 2,
    Firing = 3,
    Idling = 4
}