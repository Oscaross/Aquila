using System;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * Generic component for anything that must move towards some point. 
*/

public class Pathfinder : MonoBehaviour
{
    [Tooltip("Any asset that must flip when the sprite flips during pathfinding must be a child of this transform.")]
    [SerializeField] private Transform flipRoot;
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("How close this GameObject gets to its target vector in world units before pathfinding is complete.")]
    [SerializeField] private float arrivalThreshold = 0.1f;

    private Rigidbody2D rb;
    private Vector2 destination;
    private System.Action onArrive;
    private bool isMoving;
    private Direction facing = Direction.Left;

    public Direction Facing => facing;
    public bool IsMoving => isMoving;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    /// <summary>
    /// Moves the GameObject towards the desired location.
    /// </summary>
    /// <param name="target">The vector to move towards.</param>
    /// <param name="onArrived">The callback function that is called when the GameObject is within threshold distance of the target vector.</param>
    public void PathfindTo(Vector2 target, System.Action onArrived = null)
    {
        destination = target;
        onArrive = onArrived;
        isMoving = true;
    }

    /// <summary>
    /// Instructs the NPC to choose a random idle point between the given bounds. The NPC will then choose a random position, walk to it, then wait, then call the change callback.
    /// </summary>
    /// <param name="bounds">The minimum and maximum x coordinates that the NPC is allowed to choose for its idle point to walk to.</param>
    /// <param name="idleTimeSeconds">How long the NPC waits after it reaches its idle point.</param>
    /// <param name="onBeginIdleCycle">The function dictating what to do before the NPC starts waiting for its idle time (i.e. set the correct state for idling).</param>
    /// <param name="onFinishIdleCycle">The function dictating what to do after the NPC reaches its idle point (idle again, look for a job, etc...)</param>
    public void IdleBetween(Vector2 bounds, float idleTimeSeconds, Action onBeginIdleCycle, Action onFinishIdleCycle)
    {
        float newIdleX = Random.Range(bounds.x, bounds.y);
        onBeginIdleCycle.Invoke();
        Delay.WaitThen(
            this, 
            idleTimeSeconds, 
            () => PathfindTo(new Vector2(transform.position.y, newIdleX), onFinishIdleCycle)
            );
    }

    /// <summary>
    /// Stops a GameObject from pathfinding.
    /// </summary>
    public void StopPathfinding()
    {
        isMoving = false;
        onArrive = null;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Change the facing direction (rotation) of the GameObject.
    /// </summary>
    /// <param name="dir">The direction to face.</param>
    public void SetFacing(Direction dir)
    {
        if (dir == facing) return;

        facing = dir;
        flipRoot.localScale = new Vector3(-facing.Sign(), 1f, 1f);
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        float dx = destination.x - rb.position.x;

        if (Mathf.Abs(dx) <= arrivalThreshold)
        {
            // We've arrived.
            var callback = onArrive;
            isMoving = false;
            onArrive = null;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            callback?.Invoke();
            return;        
        }

        SetFacing(DirectionExtensions.FromDelta(dx));
        rb.linearVelocity = new Vector2(facing.Sign() * moveSpeed, rb.linearVelocity.y);
    }
}
