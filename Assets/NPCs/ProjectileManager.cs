using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public void SpawnProjectile(Vector2 origin, Vector2 target, Projectile projectilePrefab)
    {
        Projectile p = Instantiate(projectilePrefab, origin, Quaternion.identity, transform);

        p.AssignTarget(origin, target);
    }
}
