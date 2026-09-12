using UnityEngine;

/**
 * Applies movement forces to the player and controls animations.
*/

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 4f;
    [SerializeField] private float sprintingMoveSpeed = 8f;
    private Player player;
    
    private void Start()
    {
        player = GetComponentInParent<Player>();
    }
    
    private void FixedUpdate()
    {
        player.Rigidbody.linearVelocity = new Vector2(player.InputManager.MoveInput * (player.InputManager.IsSprinting ? sprintingMoveSpeed : moveSpeed), player.Rigidbody.linearVelocity.y); // the velocity is the direction (+1/-1) multiplied by the move speed which is defined at the top of the class or in the inspector
    }
}
