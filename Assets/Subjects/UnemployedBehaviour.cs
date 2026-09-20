using UnityEngine;

public class UnemployedBehaviour : ProfessionBehaviour
{
    private float idleSeconds = 4f;
    private RectInt legionBounds; // instead of using the workspace, unemployed is an edge case so we just use the legion bounds instead

    protected override void OnWorkspaceReached()
    {
        // nothing needed here
    }

    public override void OnChangedFromThisProfession()
    {
        Debug.Log("UnemployedBehaviour has been removed.");
    }

    protected override void Awake()
    {
        base.Awake();
        legionBounds = Legion.ZoneManager.GetLegionBounds();
        
        IdleLoop();
    }

    private void IdleLoop()
    {
       IdleWithin(legionBounds, idleSeconds, IdleLoop);
    }
}
