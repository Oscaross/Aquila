using UnityEngine;

/**
 * Applies movement forces to the player and controls animations.
*/

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 4f;

    [SerializeField] private float sprintingMoveSpeed = 8f;
    private Rigidbody2D rb;
    private float moveInput;
    private bool isSprinting;

    private Animator animator;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake() {  rb = GetComponent<Rigidbody2D>(); }

    // Called every time the engine draws a new frame. If the user gets 34 FPS in a given second, this calls 34 times.
    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        
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
        rb.linearVelocity = new Vector2(moveInput * ((isSprinting) ? sprintingMoveSpeed : moveSpeed), rb.linearVelocity.y); // the velocity is the direction (+1/-1) multiplied by the move speed which is defined at the top of the class or in the inspector
    }
}
