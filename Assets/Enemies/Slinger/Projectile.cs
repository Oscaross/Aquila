using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Tooltip("How long the projectile should be in flight to hit the target.")]
    [SerializeField] private float timeToHitTargetSeconds;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float maximumLifetimeSeconds = 10f;

    private float t; // the time since the projectile was first fired
    private Rigidbody2D rb;
    private Vector2 initialVelocity;
    private Vector2 origin;
    private bool isFlying;

    public System.Action OnTargetHit;

    public void AssignTarget(Vector2 from, Vector2 to)
    {
        origin = from;
        t = 0f;

        float dx = to.x - from.x;
        float dy = to.y - from.y;

        initialVelocity = new Vector2(
            dx / timeToHitTargetSeconds,
            dy / timeToHitTargetSeconds - 0.5f * gravity * timeToHitTargetSeconds
            ); // derived from SUVAT in vertical and horizontal direction

        rb.position = from;
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

        Vector2 pos = origin + new Vector2(
            initialVelocity.x * t,
            initialVelocity.y * t + 0.5f * gravity * t * t);

        rb.MovePosition(pos);

        if (t > maximumLifetimeSeconds) Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isFlying = false;
        OnTargetHit?.Invoke();
        Destroy(gameObject);
    }
}
