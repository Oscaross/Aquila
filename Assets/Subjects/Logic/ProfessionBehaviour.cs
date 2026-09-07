using UnityEngine;
using System;

public abstract class ProfessionBehaviour : MonoBehaviour
{
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
        private set;
    }

    protected Legion Legion
    {
        get;
        private set;
    }
    
    protected virtual void Awake()
    {
        Pathfinder = GetComponent<Pathfinder>();
        Subject = GetComponent<LegionSubject>();
        Legion = GetComponentInParent<Legion>();

        if (Pathfinder ==null) Debug.LogError($"{name}: no Pathfinder component.", this);
        if (Subject ==null) Debug.LogError($"{name}: no LegionSubject component.", this);
    }

    
    /// <summary>
    /// Assigns this subject to the given workspace. Handles the bookkeeping every
    /// profession shares, then hands off to the subclass for its own setup.
    /// </summary>
    public void AssignToWorkspace(SubjectWorkspace workspace)
    {
        if (workspace == null)
        {
            Debug.LogError($"{name}: cannot assign to a null workspace.", this);
            return;
        }

        if (Workspace != null) ReleaseWorkspace(); 
        
        Workspace = workspace;
        workspace.AssignWorker(Subject);

        OnWorkspaceAssigned();
    }

    protected abstract void OnWorkspaceAssigned();

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
    protected void IdleWithin(Bounds area, float idleSeconds, Action onFinished)
    {
        Vector2 point = new Vector2(
            UnityEngine.Random.Range(area.min.x, area.max.x),
            transform.position.y);

        Pathfinder.PathfindTo(point, () => Delay.WaitThen(this, idleSeconds, onFinished));
    }
}
