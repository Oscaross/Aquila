using System;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private Player player;
    
    public float MoveInput { get; private set; }
    public bool IsSprinting
    {
        get;
        private set;
    }
    
    public Direction FacingDirection { get; private set; } = Direction.Left;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    private void Update()
    {
        MoveInput = Input.GetAxisRaw("Horizontal");
        IsSprinting = Input.GetKey(KeyCode.LeftShift);
        
        player.Animator.SetFloat("Speed", Mathf.Abs(MoveInput)); // update the animator state machine to know we are now moving

        if (MoveInput != 0)
        {
            FacingDirection = (MoveInput > 0) ? Direction.Right : Direction.Left;
        }
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            player.BuildingModeManager.ToggleBuildingMode();
        }

        if (Input.GetMouseButtonDown(1))
        {
            
        }
    }
}
