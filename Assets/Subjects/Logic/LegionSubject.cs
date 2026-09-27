using System;
using JetBrains.Annotations;
using UnityEngine;

public class LegionSubject : MonoBehaviour
{
    public string Id { get; private set; }
    [SerializeField] [CanBeNull] private SubjectWorkspace currentWorkspace;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    
    public bool IsEmployed => currentWorkspace != null;

    private Legion legion;

    public float Morale
    {
        get;
        private set;
    }

    public Profession Profession
    {
        get;
        private set;
    }

    private ProfessionBehaviour behaviour;

    public SubjectVisuals Visuals
    {
        get;
        private set;
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
        
        currentWorkspace = newWorkspace;
        Profession = (newWorkspace == null) ? Profession.Unemployed : newWorkspace.RequiredProfession; // no workplace => unemployed
        
        // Fetch the definition data object for this new profession and update dependent classes
        var def = legion.SubjectManager.GetDefinitionFor(Profession);
        Visuals.Apply(def); // propogate change in profession to animation controller
        
        
        // Looks up the required class for the profession, checks it was found successfully then adds that behaviour as a component to this GameObject.
        // This means that our subject keeps all of its normal attributes but is effectively swapped, 
        
        behaviour = Profession.AddBehaviour(gameObject);
        
        behaviour.AssignToWorkspace(currentWorkspace);
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


