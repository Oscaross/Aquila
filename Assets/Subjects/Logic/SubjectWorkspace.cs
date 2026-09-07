using JetBrains.Annotations;
using UnityEngine;

public abstract class SubjectWorkspace : MonoBehaviour
{
    private Legion legion;
    [Tooltip("The distance in world units that the workspace's workable area should be in total along the x axis.")]
    [SerializeField] private float xSize;

    public abstract Profession RequiredProfession { get; }
    
    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
        legion.SubjectManager.RegisterJob(this);
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

    public Bounds Bounds => new(transform.position, new Vector3(xSize, 1f, 0f));

    public void AssignWorker(LegionSubject subject)
    {
        if (IsCurrentlyWorked)
        {
            Debug.LogError("Can't assign a worker to a workspace that is already being worked.", this);
        }
        
        WorkerWhoWorksThis = subject;
    }
    
    [ContextMenu("Unassign Worker")]
    public void UnassignWorker()
    {
        if (WorkerWhoWorksThis == null)
        {
            Debug.LogError("Can't fire a worker if they don't work this workspace.", this);
            return;
        }
        
        WorkerWhoWorksThis.WellThisIsItIHaveBeenFired();
        WorkerWhoWorksThis = null;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = IsCurrentlyWorked ? Color.green : Color.red;
        Bounds b = Bounds;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
