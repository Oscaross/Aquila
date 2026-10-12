using System;
using JetBrains.Annotations;
using UnityEngine;

public class LegionSubject : MonoBehaviour, IEntity<LegionSubjectModel>, IFindable
{
    public int Id => data.Id;
    [SerializeField] [CanBeNull] private SubjectWorkspace currentWorkspace;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;

    private LegionSubjectModel data;

    public void Bind(LegionSubjectModel model, WorldContext context)
    {
        data = model;
        // Try resolve the instance of the workspace we were assigned to, if none exists then this gives us no workspace so it'll remain null
        if (context.Registry.TryGet(data.CurrentWorkspaceID, out Building buildingWithWorkspace)) currentWorkspace = buildingWithWorkspace.GetComponent<SubjectWorkspace>();

        transform.position = new Vector3(data.XPos, data.YPos, 0f);
    }

    public bool IsEmployed => currentWorkspace != null;

    private Legion legion;

    public float Morale => data.Morale;

    public Profession Profession =>
        (IsEmployed) ? currentWorkspace.RequiredProfession : Profession.Unemployed;

    private ProfessionBehaviour behaviour;

    public SubjectVisuals Visuals
    {
        get;
        private set;
    }

    private void FixedUpdate()
    {
        data.XPos = transform.position.x;
        data.YPos = transform.position.y;
    }

    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
        Visuals = GetComponentInChildren<SubjectVisuals>();
        
        if (Visuals == null) Debug.LogError("Subject unable to fetch a reference to it's visuals object! Did you change the tree structure? (visuals object needs to be a child of the subject).");
    }

    /// <summary>
    /// Assigns the worker to a new workspace and profession.
    /// </summary>
    /// <param name="newWorkspace">The reference to the workspace, which must derive from SubjectWorkspace. The argument is nullable, and if it is null then the profession is set to unemployed.</param>
    public void ChangeProfession(SubjectWorkspace newWorkspace)
    {
        // Deactivate all Profession related scripts from before
        var professionScripts = GetComponentsInChildren<ProfessionBehaviour>();

        foreach (var script in professionScripts)
        {
            script.OnChangedFromThisProfession();
            script.enabled = false;
            Destroy(script);
        }

        data.CurrentWorkspaceID = 0;
        
        // Fetch the definition data object for this new profession and update dependent classes
        var def = legion.SubjectManager.GetDefinitionFor(Profession);
        Visuals.Apply(def); // propogate change in profession to animation controller
        
        // Looks up the required class for the profession, checks it was found successfully then adds that behaviour as a component to this GameObject.
        // This means that our subject keeps all of its normal attributes but is effectively swapped, 
        
        behaviour = Profession.AddBehaviour(gameObject);
        
        behaviour.AssignToWorkspace(newWorkspace);
        gameObject.name = Profession.ToString();
    }
    
    /// <summary>
    /// Release a worker from their responsibilities and make them unemployed.
    /// </summary>
    public void MakeUnemployed()
    {
        ChangeProfession(null);
    }
}

public class LegionSubjectModel : EntityData
{
    public LegionSubjectModel(float xPos, float yPos, int currentWorkspaceID, float morale)
    {
        XPos = xPos;
        YPos = yPos;
        CurrentWorkspaceID = currentWorkspaceID;
        Morale = morale;
    }
    
    public float XPos;
    public float YPos;
    public int CurrentWorkspaceID;
    public float Morale;
}


