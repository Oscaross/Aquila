using UnityEngine;

public class UnemployedBehaviour : ProfessionBehaviour
{
    private float idleSeconds = 4f;
    protected override void OnWorkspaceAssigned()
    {
        // Empty, we don't assign workspaces to the unemployed NPC.
    }

    protected override void Awake()
    {
        base.Awake();
        IdleLoop();
    }

    private void IdleLoop()
    {
       //  IdleWithin(Legion.LegionBounds, idleSeconds, IdleLoop);
    }
}
