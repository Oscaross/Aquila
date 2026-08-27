using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public void ShootProjectileAtTarget(Vector2 origin, Vector2 target, float firerAccuracy, Projectile projectilePrefab, System.Action<Vector2> onShotMissedTarget, System.Action<Collider2D, Vector2> onShotHitTarget)
    {
        Projectile p = Instantiate(projectilePrefab, origin, Quaternion.identity, transform);
        p.TryFireProjectile(origin, target, firerAccuracy, onShotMissedTarget, onShotHitTarget);
    }
}
