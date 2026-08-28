using System;
using UnityEngine;

public class RangedAttacker : MonoBehaviour
{
    [SerializeField] private ProjectileProfile profileThisFires;
    [SerializeField] private Transform firePoint;
    [SerializeField, Range(0f, 1f)] private float accuracy = 0.9f;
    [SerializeField] private float cacheTolerance = 0.01f;
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private LayerMask targetLayerMask;

    private Legion legion;
    private float? cachedThetaRad;
    private Vector2 origin, target;
    private Vector2 initialVelocity;

    private Action onTargetMissed;
    private Action onTargetHit;
    
    
    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
    }

    public bool TryFireAt(Vector2 at,
        Action onHit = null, Action onMiss = null)
    {
        onTargetHit = onHit;
        onTargetMissed = onMiss;

        if (!IsCacheValid(firePoint.position, at))
        {
            if (!TrySolveTheta(firePoint.position, at, out float thetaRad)) return false;
            cachedThetaRad = thetaRad;
        }

        origin = firePoint.position;
        target = at;

        // Jitter per shot, then re-solve the velocity for the cached angle. Cheap: no linecasts.
        Vector2 aim = BallisticSolver.ApplyScatter(origin, target, accuracy, profileThisFires);
        if (!BallisticSolver.TrySolveVelocity(origin, aim, cachedThetaRad.Value,
                profileThisFires.gravity,
                profileThisFires.maxSpeed, out initialVelocity))
            return false;

        FireProjectile();
        return true;
    }
    
    /// <summary>
    /// Sweeps the profile's angle band shallowest-first and returns the first launch angle that
    /// both reaches the target and has an unobstructed trajectory. Expensive as it linecasts with each candidate so the caller caches the result until the firer or target moves.
    /// </summary>
    /// <param name="to">The true target, not a jittered one, so the cached angle stays valid.</param>
    private bool TrySolveTheta(Vector2 from, Vector2 to, out float thetaRad)
    {
        thetaRad = 0f;

        float minDeg = profileThisFires.minProjectionAngleDeg;
        float maxDeg = profileThisFires.maxProjectionAngleDeg;
        float stepDeg = profileThisFires.angleStepDegs;

        if (stepDeg <= 0f || maxDeg < minDeg) return false; // malformed profile

        int intervals = Mathf.FloorToInt((maxDeg - minDeg) / stepDeg);

        // Ascending, so the first hit is the flattest viable shot.
        for (int i = 0; i <= intervals; i++)
        {
            float candidateRad = (minDeg + i * stepDeg) * Mathf.Deg2Rad;

            if (!BallisticSolver.TrySolveVelocity(from, to, candidateRad, profileThisFires.gravity,
                    profileThisFires.maxSpeed, out Vector2 u))
                continue; // unreachable at this angle, or needs more speed than we have

            if (!BallisticSolver.IsPathClear(from, to, u, profileThisFires.gravity, obstacleLayerMask))
                continue; // something in the way

            thetaRad = candidateRad;
            return true;
        }

        return false;
    }

    private void FireProjectile()
    {
        var args = new ProjectileArgs(origin, initialVelocity, profileThisFires, targetLayerMask, obstacleLayerMask, HandleHit, HandleMiss);
            
        legion.ProjectileManager.FireProjectile(args); 
    }

    private void HandleHit(Collider2D hitCollider, Vector2 hitPos)
    {
        onTargetHit?.Invoke(); // call the upstream handler (i.e. the Archer NPC) so they can handle NPC-side hit logic
        Debug.Log("Hit!"); 
    }

    private void HandleMiss(Vector2 missPos)
    {
        onTargetMissed?.Invoke();
        Debug.Log("Miss!");
    }

    private bool IsCacheValid(Vector2 newOrigin, Vector2 newTarget) => cachedThetaRad != null && Vector2.Distance(newOrigin, origin) <= cacheTolerance && Vector2.Distance(newTarget, target) <= cacheTolerance;
}
