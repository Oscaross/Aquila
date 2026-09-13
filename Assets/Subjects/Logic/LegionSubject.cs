using System;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class LegionSubject : MonoBehaviour
{
    public string Id { get; private set; }
    [SerializeField] [CanBeNull] private SubjectWorkspace currentWorkspace;
    public bool IsEmployed => currentWorkspace != null;

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

    public void WitnessMoraleEvent(MoraleEvent e)
    {
        
    }

    public void ChangeProfession(SubjectWorkspace newWorkspace)
    {
        currentWorkspace = newWorkspace;
        Profession = newWorkspace.RequiredProfession;
        
        // Looks up the required class for the profession, checks it was found successfully then adds that behaviour as a component to this GameObject.
        // This means that our subject keeps all of its normal attributes but is effectively swapped, 
        Type behaviourType = ProfessionExtensions.TypeFor(newWorkspace.RequiredProfession);
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
    public void WellThisIsItIHaveBeenFired()
    {
        Type unemployed = ProfessionExtensions.TypeFor(Profession.Unemployed);
        Behaviour = (ProfessionBehaviour) gameObject.AddComponent(unemployed);
        gameObject.name = Profession.ToString();

        currentWorkspace = null;
    }
}


