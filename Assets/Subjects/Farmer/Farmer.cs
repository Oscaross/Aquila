using System;
using JetBrains.Annotations;
using UnityEngine;

public class Farmer : MonoBehaviour
{
    private Pathfinder pathfinder;
    private Animator animator;
    private bool isCurrentlyWorkingField;

    [Header("Tunables")] 
    [SerializeField] private float idleTimeSeconds;

    [SerializeField] private float harvestAnimationSeconds;
    
    [Header("Read only")]
    [SerializeField] private FarmerState currentState;
    
    [CanBeNull] private FarmField workingField;

    private void SetState(FarmerState next)
    {
        animator.SetInteger("State", (int) next);
        currentState = next;
    }
    
    private void Awake()
    {
        pathfinder = GetComponent<Pathfinder>();
        animator = GetComponent<Animator>();
    }

    private void AssignToField(FarmField field)
    {
        if (field == null) Debug.LogError("Assignment to field attempted that doesn't exist.", this);
        workingField = field;
        field.AssignFarmerToField();
        
        // Begin walking to our job; get the middle of the field, walk to it and go into walking mode.
        Vector2 fieldMidpoint = new Vector2(Mathf.Lerp(field.FieldBoundsX.x, field.FieldBoundsX.y, 0.5f), 0f);
        pathfinder.PathfindTo(fieldMidpoint, BeginFarmingField);
        SetState(FarmerState.Walking);
    }

    private void UnassignFromField()
    {
        if (workingField == null) Debug.LogError("Can't unassign farmer when they are not assigned to a field.", this);

        workingField.UnassignFarmerFromField();
    }
    
    private void BeginFarmingField()
    {
        isCurrentlyWorkingField = true;
        
        IdleLoop();
    }

    private void IdleLoop()
    {
        Debug.Assert(workingField != null);
        
        pathfinder.IdleBetween(workingField.FieldBoundsX, idleTimeSeconds,
            () => SetState(FarmerState.Idling),
            IdleLoop
            );
    }
    
    private void DoHarvest(float baseYield)
    {
        Debug.Log($"Harvest success - yield {baseYield}");
        SetState(FarmerState.Harvesting);
        workingField.FarmParent.StoreGrain(baseYield);

        Delay.WaitThen(this, harvestAnimationSeconds, BeginFarmingField);
    }
}

enum FarmerState
{
    Idling,
    Walking,
    Harvesting,
    Transporting
}
