using System;
using JetBrains.Annotations;
using UnityEngine;

public class LegionSubject : MonoBehaviour
{
    public string Id { get; private set; }
    [SerializeField] [CanBeNull] private SubjectWorkspace currentWorkspace;
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

    public ProfessionBehaviour Behaviour
    {
        get;
        private set;
    }

    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
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
        
        // Looks up the required class for the profession, checks it was found successfully then adds that behaviour as a component to this GameObject.
        // This means that our subject keeps all of its normal attributes but is effectively swapped, 
        
        // We're now going to fetch the single profession behaviour we need for our new profession and assign it to this game object.
        
        Type behaviourType = ProfessionExtensions.TypeFor(Profession);
        if (behaviourType == null)
        {
            Debug.LogError("Subject profession has no corresponding class telling it how to act in that profession or failed to fetch this class!", this);
            return;
        }

        Behaviour = (ProfessionBehaviour)gameObject.AddComponent(behaviourType);
        Behaviour.AssignToWorkspace(currentWorkspace);
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


