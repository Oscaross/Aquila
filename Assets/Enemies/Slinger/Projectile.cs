using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float gravity = -6f;
    [Tooltip("The maximum number of seconds a projectile can exist in flight before being despawned.")]
    [SerializeField] private float maximumLifetimeSeconds = 10f;
    [Tooltip("The number of seconds after colliding with some other Rigidbody that the projectile remains in the world.")]
    [SerializeField] private float despawnTimeAfterHitSeconds = 10f;
    [SerializeField] private float thetaMin;
    [SerializeField] private float thetaMax;
    [Tooltip("Anything that will stop the projectile from flying on its planned trajectory.")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Anything that the projectile is aiming for (i.e. if it collides with this layer it will count it as a hit and proceed with the hit logic).")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float minSpreadRadius;
    [SerializeField] private float spreadAtReferenceRange;
    [SerializeField] private float spreadFalloff;
    [SerializeField] private float verticalSpreadMultiplier;
    [SerializeField] private float maxLaunchSpeed;
    [SerializeField] private float referenceRange;

    [Tooltip("The number of degrees between each theta trajectory tested; e.g. if I had a range of 30* to 45* and this was 5 then the computer samples paths projected at [30, 35, 40, 45]")]
    [SerializeField] private int angleStepDegs;
    [Tooltip("How many samples we take (evenly spread) across the trajectory. If any sample collides with some collider then the path is considered \"blocked\"")]
    [SerializeField] private int samplesPerPath;
    

    private float t; // the time in seconds since the projectile was first fired
    private Rigidbody2D rb;
    private Vector2 initialVelocity;
    private Vector2 origin;
    private Vector2 target;
    private bool isFlying;
    private bool hasPathBeenComputed; // have we already done the work to find a path


    public System.Action<Collider2D, Vector2> OnShotHitTarget;
    public System.Action<Vector2> OnShotMissedTarget;

    public bool TryFireProjectile(Vector2 origin, Vector2 target, float firerAccuracy, System.Action<Vector2> onShotMissedTarget, System.Action<Collider2D, Vector2> onShotHitTarget)
    {
        // Use the cached theta rather than doing our expensive pathfinding again
        if (hasPathBeenComputed)
        {
            Fire();
            return true;
        }

        this.origin = origin;
        OnShotHitTarget = onShotHitTarget;
        OnShotMissedTarget = onShotMissedTarget;

        // Apply our jitter to the target according to the firer accuracy and the distance between the two.
        this.target = ApplyScatter(origin, target, firerAccuracy);

        // Create a discrete array of candidate angle solutions split by angleStepDegs from min => max.
        // i.e. min = 20, max = 30 with step = 5 gives [20, 25, 30].
        int intervals = Mathf.FloorToInt((thetaMax - thetaMin) / angleStepDegs);
        List<float> candidates = new List<float>();

        for (int i = 0; i <= intervals; i++)
        {
            float candidate = thetaMin + i * angleStepDegs;

            // Is this angle achievable within our speed limits and required trajectory?
            if (!TrySolveVelocity(candidate * Mathf.Deg2Rad, out Vector2 v)) continue;
            if (DoesTrajectoryHitObstacle(v)) continue;

            candidates.Add(candidate);
        }

        if (candidates.Count == 0) return false; // oh no! There aren't any valid angles that this projectile could be fired from to hit the target.

        float theta = candidates.Min(); // the smallest angle is the always chosen one
        TrySolveVelocity(theta, out Vector2 requiredVelocity); // theta has been calculated so resolve the required velocity

        initialVelocity = requiredVelocity;
        hasPathBeenComputed = true;
        Fire();
        return true;
    }

    private Vector2 ApplyScatter(Vector2 origin, Vector2 target, float firerAccuracy)
    {
        float inaccuracy = 1f - Mathf.Clamp01(firerAccuracy); 
        if (inaccuracy <= 0f) return target; // i.e. a firer with 1f accuracy has 0f inaccuracy so they just always hit the target

        float distance = Vector2.Distance(origin, target);
        float radius = inaccuracy * inaccuracy * (minSpreadRadius + spreadAtReferenceRange * Mathf.Pow(distance / referenceRange, spreadFalloff));

        Vector2 offset = (Random.insideUnitCircle + Random.insideUnitCircle) * 0.5f;
        offset.y *= verticalSpreadMultiplier;

        return target + offset * radius;
    }

    private bool DoesTrajectoryHitObstacle(Vector2 u)
    {
        // Split the horizontal area between target and origin into discrete x steps 
        float totalDx = target.x - origin.x;
        float stepDx = totalDx / samplesPerPath;

        Vector2 prev = origin;

        for (int i = 1; i <= samplesPerPath; i++)
        {
            float relX = stepDx * i; // the distance in x into the arc trajectory we've travelled
            Vector2 curr = new Vector2(origin.x + relX, origin.y + ProjectYPos(relX, u));

            // N.B a linecast is a straight line between sample i and sample i+1. The straight line checks if it intersects with anything and if it does, it does not fire.
            if (Physics2D.Linecast(prev, curr, obstacleMask)) return true; // we hit something in the obstacle mask so this does hit an obstacle

            prev = curr;
        }

        return false;
    }

    private void Fire()
    {
        t = 0;
        isFlying = true;
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void FixedUpdate()
    {
        if (!isFlying) return;

        t += Time.fixedDeltaTime;

        float relX = ProjectXPos();
        Vector2 pos = origin + new Vector2(relX, ProjectYPos(relX, initialVelocity));
        rb.MovePosition(pos);

        if (t > maximumLifetimeSeconds)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int layerBit = 1 << collision.gameObject.layer;
        Vector2 impactPoint = collision.GetContact(0).point;

        // We hit the target if the collider's LayerMask contains the target layer
        if ((targetMask.value & layerBit) != 0)
        {
            isFlying = false;
            OnShotHitTarget?.Invoke(collision.collider, impactPoint);
            Destroy(gameObject);
        }
        // We missed the target if the collider's LayerMask contains the obstacle layer but not the target layer
        else if ((obstacleMask.value & layerBit) != 0)
        {
            isFlying = false;
            OnShotMissedTarget?.Invoke(impactPoint);
            Destroy(gameObject);
        }
    }

    private bool TrySolveVelocity(float thetaRad, out Vector2 u)
    {
        u = default;
        float dx = target.x - origin.x;
        float dy = target.y - origin.y;
        float absDx = Mathf.Abs(dx);
        if (absDx < 0.001f) return false;

        float cos = Mathf.Cos(thetaRad);
        float denom = 2f * cos * cos * (absDx * Mathf.Tan(thetaRad) - dy);
        if (denom <= 0f) return false;

        float s = Mathf.Sqrt((-gravity * absDx * absDx) / denom);
        if (s > maxLaunchSpeed) return false;

        u = new Vector2(Mathf.Sign(dx) * s * cos, s * Mathf.Sin(thetaRad));
        return true;
    }

    /// <summary>
    /// Vertical displacement from origin.
    /// </summary>
    private float ProjectYPos(float x, Vector2 u)
    {
        float ux = u.x;
        return (u.y / ux) * x + (0.5f * gravity / (ux * ux)) * x * x;
    }

    /// <summary>
    /// Horizontal displacement from origin.
    /// </summary>
    private float ProjectXPos() => t * initialVelocity.x;
}
