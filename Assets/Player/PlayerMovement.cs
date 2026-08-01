using UnityEngine;

/**
 * Applies movement forces to the player and controls animations.
*/

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private float moveInput;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake() {  rb = GetComponent<Rigidbody2D>(); }

    // Called every time the engine draws a new frame. If the user gets 34 FPS in a given second, this calls 34 times.
    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        animator.SetFloat("Speed", Mathf.Abs(moveInput)); // update the animator state machine to know we are now moving

        if (moveInput != 0)
        {
            transform.localScale = new Vector3(-Mathf.Sign(moveInput) * 1.5f, 1.5f, 1f); // make the character face left or right depending on the movement direction last pressed
        }
    }

    // Called at a stable rate (60/s). Regardless of FPS/computer lag, this calls at the same rate, means stable physics.
    private void FixedUpdate()
    {
        // Actually moves the character left/right.
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); // the velocity is the direction (+1/-1) multiplied by the move speed which is defined at the top of the class or in the inspector
    }
}
