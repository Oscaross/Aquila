using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Slinger : MonoBehaviour
{
    [SerializeField] private SlingerState currentState;
    [SerializeField] private Projectile pebble;
    // [SerializeField] private Animator animator;
    [Tooltip("Time elapsed between the slinger beginning the slingshot reload and starting the firing animation in seconds.")]
    [SerializeField] private float loadingTimeSeconds;
    [Tooltip("Time elapsed between the slinger starting the firing animation and the projectile being released onto its trajectory in seconds.")]
    [SerializeField] private float firingTimeSeconds;
    [Tooltip("Time elapsed between slinger finishing firing and starting reloading for the next shot.")]
    [SerializeField] private float betweenShotsTimeSeconds;

    [Tooltip("DEBUG ONLY: Test the projectile firing system")]
    [SerializeField] private float aimingPointX;
    [Tooltip("The distance in world units that the slinger should stand away from its actual target.")]
    [SerializeField] private float targetStandingRange;
    [Tooltip("Where projectiles originate from on the slinger.")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float baseAccuracy;

    private Pathfinder pathfinder;
    private Legion legion;
    private Vector2 currentTarget;

    private void Awake()
    {
        pathfinder = GetComponent<Pathfinder>();
        legion = GetComponentInParent<Legion>();
    }

    private void SetState(SlingerState next)
    {
        currentState = next;
        // animator.SetInteger("State", (int)next);
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
            legion.ProjectileManager.ShootProjectileAtTarget(transform.position, currentTarget, baseAccuracy, pebble, OnTargetMiss, OnTargetHit);
            SetState(SlingerState.Idling);
            Delay.WaitThen(this, betweenShotsTimeSeconds, LoadSlingshot);
        });
    }

    private void OnTargetMiss(Vector2 contactPoint)
    {
        Debug.Log("Oh no! We missed.");
    }

    private void OnTargetHit(Collider2D collider, Vector2 contactPoint)
    {
        Debug.Log("Oh yo! We hit.");
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