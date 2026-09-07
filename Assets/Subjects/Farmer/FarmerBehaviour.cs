using System;
using UnityEngine;

/// <summary>
/// Controls the logic and behaviour for the Farmer class.
/// Farmers are expected to idle between the bounds of their field, playing a harvest animation when the fields are ready for harvest.
/// </summary>

public class FarmerBehaviour : ProfessionBehaviour
{
    [SerializeField] private float idleSeconds = 6f;

    protected override void OnWorkspaceAssigned() => Pathfinder.PathfindTo(Workspace.Bounds.center, IdleLoop);

    private void IdleLoop() => IdleWithin(Workspace.Bounds, idleSeconds, IdleLoop);
}
