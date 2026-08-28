using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Object that encapsulates all information about how this specific type of projectile (i.e. arrow/pebble/spear) should behave.
    private ProjectileProfile profile;
    // Object that encapsulates all information about how this specific INSTANCE of projectile should behave, such as where it was launched from or its projection angle.
    private ProjectileArgs args;
    
    private float t; // the time in seconds since the projectile was first fired
    private bool isFlying;
    private bool hasExpired;
    private Rigidbody2D rb;

    public System.Action<Projectile> OnExpired;

    public void Initialise(ProjectileProfile profile, ProjectileArgs args)
    {
        t = 0;
        isFlying = true;
        this.args = args;
        this.profile = profile;

        rb.position = args.Origin;
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

        Vector2 pos = BallisticSolver.GetWorldPositionAtTime(args.Origin, t, args.InitialVelocity, profile.gravity);
        rb.MovePosition(pos);

        if (t > profile.maximumLifetimeSeconds)
        {
            Expire();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int layerBit = 1 << collision.gameObject.layer;
        Vector2 impactPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : rb.position;

        // We hit the target if the collider's LayerMask contains the target layer
        if ((args.TargetMask.value & layerBit) != 0)
        {
            args.OnHit?.Invoke(collision.collider, impactPoint);

            Delay.WaitThen(this, profile.despawnTimeAfterHitSeconds, () => Expire());
        }
        // We missed the target if the collider's LayerMask contains the obstacle layer but not the target layer
        else if ((args.ObstacleMask.value & layerBit) != 0)
        {
            args.OnMiss?.Invoke(impactPoint);
            Expire();
        }
    }

    private void Expire()
    {
        isFlying = false;
        hasExpired = true;
        OnExpired.Invoke(this);
    }
}