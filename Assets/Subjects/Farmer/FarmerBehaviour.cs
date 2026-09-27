using UnityEngine;

/// <summary>
/// Controls the logic and behaviour for the Farmer class.
/// Farmers are expected to idle between the bounds of their field, playing a harvest animation when the fields are ready for harvest.
/// </summary>

public class FarmerBehaviour : ProfessionBehaviour
{
    [SerializeField] private float idleSeconds = 6f;
    [SerializeField] private SoundDefinition harvestAnimationSound;

    private void OnDisable()
    {
        Workspace.GetComponent<FarmField>().OnHarvestReady -= Harvest;
    }

    protected override void OnWorkspaceReached()
    {
        Pathfinder.PathfindTo(Workspace.WorkingBounds.center, ProfessionDefinition.walkSpeed, IdleLoop);
        Workspace.GetComponent<FarmField>().OnHarvestReady += Harvest; // subscribe to the harvest method so the field can alert this farmer when the crops are ready
    }
    

    public override void OnChangedFromThisProfession()
    {
        
    }

    private void IdleLoop() => IdleWithin(Workspace.WorkingBounds, idleSeconds, IdleLoop);

    private void Harvest(float yield)
    {
        // AudioManager.Instance.PlayOneShot(harvestAnimationSound, transform.position);
        Subject.Visuals.TriggerWorkAnimation();
    }
}
