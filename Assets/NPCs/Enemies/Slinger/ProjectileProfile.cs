using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileProfile", menuName = "Scriptable Objects/Projectiles")]
public class ProjectileProfile : ScriptableObject
{
    public Projectile projectilePrefab;
    public float gravity;
    public float maxSpeed;
    public float minProjectionAngleDeg;
    public float maxProjectionAngleDeg;
    public float angleStepDegs;
    [Tooltip("The maximum number of seconds a projectile can exist in flight before being despawned.")]
    public float maximumLifetimeSeconds = 10f;
    [Tooltip("The number of seconds after colliding with some other Rigidbody that the projectile remains in the world.")]
    public float despawnTimeAfterHitSeconds = 10f;
}
