using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement movementSystem;
    [SerializeField] private BuildingModeManager buildingModeManager;
    [SerializeField] private PlayerInput inputManager;
    [SerializeField] private Animator animator;
    
    public PlayerMovement MovementSystem => movementSystem;
    public BuildingModeManager BuildingModeManager => buildingModeManager;
    public PlayerInput InputManager => inputManager;
    public Animator Animator => animator;
    public Rigidbody2D Rigidbody { get; private set; }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -inputManager.FacingDirection.Sign();
        transform.localScale = scale;
    }
}
