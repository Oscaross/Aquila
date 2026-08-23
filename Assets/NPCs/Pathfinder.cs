using System;
using UnityEngine;

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
    private int facing = 1;

    /// <summary>
    /// -1 for left and +1 for right.
    /// </summary>
    public int Facing => facing;
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
    /// <param name="dir">-1 for left, +1 for right, anything else will log an error.</param>
    public void SetFacing(int dir)
    {
        if (dir == facing) return;
        if (Mathf.Abs(dir) != 1)
        {
            Debug.LogError("A pathfinder was told to face a direction other than -1 or +1.");
            return;
        }

        facing = dir;
        flipRoot.localScale = new Vector3(-facing, 1f, 1f);
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

        SetFacing((int)Mathf.Sign(dx));
        rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);
    }
}
