using UnityEngine;
using System;

public abstract class ProfessionBehaviour : MonoBehaviour
{
    private Gait gait;
    protected Gait Gait
    {
        get => gait;
        private set
        {
            gait = value;
            Subject.Visuals.RefreshAnimations(gait);
        }
    }
    
    protected Pathfinder Pathfinder
    {
        get;
        private set;
    }

    protected LegionSubject Subject
    {
        get;
        private set;
    }

    protected SubjectWorkspace Workspace
    {
        get;
        set;
    }

    protected Legion Legion
    {
        get;
        private set;
    }

    protected ProfessionDefinition ProfessionDefinition
    {
        get;
        private set;
    }

    protected virtual void Awake()
    {
        Pathfinder = GetComponent<Pathfinder>();
        Subject = GetComponent<LegionSubject>();
        Legion = GetComponentInParent<Legion>();

        if (Pathfinder == null) Debug.LogError($"{name}: no Pathfinder component.", this);
        if (Subject == null) Debug.LogError($"{name}: no LegionSubject component.", this);
        
        ProfessionDefinition = Legion.SubjectManager.GetDefinitionFor(Subject.Profession);
    }

    /// <summary>
    /// Assigns this subject to the given workspace. Handles the bookkeeping every
    /// profession shares, then hands off to the subclass for its own setup.
    /// </summary>
    public void AssignToWorkspace(SubjectWorkspace workspace)
    {
        if (Workspace != null) ReleaseWorkspace();

        Pathfinder.StopPathfinding(); // prevent the pathfinder from calling anything else
        Workspace = workspace;

        if (workspace == null)
        {
            // TODO: For now this'll just walk everyone back to the center. We soon want to get the idle loop mechanism working here too
            Gait = Gait.Walk;
            Pathfinder.PathfindTo(Legion.ZoneManager.GetLegionBounds().center, ProfessionDefinition.walkSpeed);
        }
        else
        {
            // Below gets us around the issue of a workspace thinking it has a worker before the worker is physically at it yet
            Gait = Gait.Run;
            
            Pathfinder.PathfindTo(Workspace.WorkingBounds.center, ProfessionDefinition.runSpeed, () =>
            {
                // Once we reach the job, officially assign the worker
                workspace.AssignWorker(Subject);
                OnWorkspaceReached();
                Gait = Gait.Idle;
            });
        }
    }

    protected abstract void OnWorkspaceReached();
    public abstract void OnChangedFromThisProfession();

    protected void ReleaseWorkspace()
    {
        if (Workspace == null) return;
        Workspace.UnassignWorker();
        Workspace = null;
    }
    
    /// <summary>
    /// Walks to a random point within the area, waits, then calls back. Shared by every
    /// profession — the wandering behaviour is the same regardless of what the NPC does
    /// for work.
    /// </summary>
    protected void IdleWithin(RectInt area, float idleSeconds, Action onFinished)
    {
        Vector2 point = new Vector2(
            UnityEngine.Random.Range(area.min.x, area.max.x),
            transform.position.y);

        Gait = Gait.Walk;
        Pathfinder.PathfindTo(point, ProfessionDefinition.walkSpeed, () =>
        {
            Gait = Gait.Idle;
            Delay.WaitThen(this, idleSeconds, onFinished);
        });
    }
}
