using System;
using UnityEngine;

/// <summary>
/// Controls the logic and behaviour for the Farmer class.
/// Farmers are expected to idle between the bounds of their field, playing a harvest animation when the fields are ready for harvest.
/// </summary>

public class FarmerBehaviour : ProfessionBehaviour
{
    [SerializeField] private float idleSeconds = 6f;

    protected override void OnWorkspaceReached() => Pathfinder.PathfindTo(Workspace.WorkingBounds.center, IdleLoop);

    public override void OnChangedFromThisProfession()
    {
        
    }

    private void IdleLoop() => IdleWithin(Workspace.WorkingBounds, idleSeconds, IdleLoop);
}
