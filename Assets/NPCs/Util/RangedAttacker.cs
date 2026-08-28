using System.Runtime.CompilerServices;
using UnityEngine;

public class RangedAttacker : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Range(0f, 1f)] private float accuracy = 0.9f;
    [SerializeField] private float cacheTolerance = 0.01f;

    private float? cachedThetaRad;
    private Vector2 origin, target;

    public bool TryFireProjectile(Vector2 origin, Vector2 target, System.Action<Vector2> OnTargetMissed, System.Action<Vector2, Collider2D> OnTargetHit)
    {
        if (IsCacheValid(origin, target))
        {
            FireProjectile();
            return true;
        }
    }

    private void FireProjectile()

    private bool IsCacheValid(Vector2 newOrigin, Vector2 newTarget) => cachedThetaRad != null && Vector2.Distance(newOrigin, origin) <= cacheTolerance && Vector2.Distance(newTarget, target) <= cacheTolerance;
}
