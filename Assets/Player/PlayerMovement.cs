using UnityEngine;
using UnityEngine.InputSystem;

/**
 * Applies movement forces to the player and controls animations.
*/

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 4f;
    [SerializeField] private float sprintingMoveSpeed = 8f;
    private Player player;

    private InputActionMap gameplayActionMap;

    private InputAction move;
    private InputAction sprint;
    
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        gameplayActionMap = InputSystem.actions.FindActionMap("Gameplay", true);
        
        move = gameplayActionMap.FindAction("Move");
        sprint = gameplayActionMap.FindAction("Sprint");
    }
    
    private void FixedUpdate()
    {
        float axis = move.ReadValue<float>();
        player.Rigidbody.linearVelocity = new Vector2(axis * (sprint.inProgress ? sprintingMoveSpeed : moveSpeed), player.Rigidbody.linearVelocity.y); // the velocity is the direction (+1/-1) multiplied by the move speed which is defined at the top of the class or in the inspector
        
        if (axis != 0f) player.SetFacing(DirectionExtensions.FromDelta(axis));

        player.Animator.SetBool(IsMovingHash, axis != 0);
        player.Animator.SetBool(IsSprintingHash,  sprint.inProgress);
    }
}
