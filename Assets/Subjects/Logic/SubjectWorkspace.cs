using JetBrains.Annotations;
using UnityEngine;

[RequireComponent(typeof(Building))]
public abstract class SubjectWorkspace : MonoBehaviour
{
    public Building Building => building;
    private Building building;

    private Legion legion;
    
    public abstract Profession RequiredProfession { get; }
    public abstract RectInt WorkingBounds { get; }
    
    /// <summary>
    /// Allows workspaces to change state over time. Automatically called from a single manager object and provides the game time that has elapsed since last frame.
    /// </summary>
    /// <param name="deltaGameSeconds"></param>
    public abstract void Tick(float deltaGameSeconds);
    
    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
        building = GetComponent<Building>();
    }

    /// <summary>
    /// Is a worker working on this workspace?
    /// </summary>
    public bool IsCurrentlyWorked => WorkerWhoWorksThis != null;

    /// <summary>
    /// The current worker who works this.
    /// </summary>
    [CanBeNull]
    public LegionSubject WorkerWhoWorksThis
    {
        get;
        private set;
    }

    public void AssignWorker(LegionSubject subject)
    {
        Debug.Log($"Assigning worker to {gameObject.name}");
        
        if (IsCurrentlyWorked)
        {
            Debug.LogError("Can't assign a worker to a workspace that is already being worked.", this);
            return;
        }
        
        WorkerWhoWorksThis = subject;
    }

    protected abstract void OnWorkerAssigned();
    
    [ContextMenu("Unassign Worker")]
    public void UnassignWorker()
    {
        if (WorkerWhoWorksThis == null)
        {
            Debug.LogError("Can't fire a worker if they don't work this workspace.", this);
            return;
        }
        
        WorkerWhoWorksThis.MakeUnemployed();
        WorkerWhoWorksThis = null;
    }
}
