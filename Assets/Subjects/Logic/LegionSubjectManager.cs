using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Responsible for assigning workers to job sites when the demand presents itself.
/// </summary>

public class LegionSubjectManager : MonoBehaviour
{
    [Tooltip("The basic prefab that is instantiated for an unemployed subject. This prefab lives until the NPC dies, and persists between career changes.")]
    [SerializeField] private LegionSubject subjectPrefab;
    [Tooltip("Customisable parameters that exist for each profession, such as visuals, spritesheets, move speeds, health, etc...")]
    [SerializeField] private ProfessionDefinition[] professionDefinitions;
    
    [Header("READ-ONLY")]
    [SerializeField] private List<LegionSubject> subjects = new();
    private Queue<SubjectWorkspace> jobQueue = new();

    private void Awake()
    {
        // TODO: Debug we just make a couple of guys for different professions at the origin
        for (int i = 0; i < 10; i++)
        {
            CreateNewSubject();
        }

        foreach (LegionSubject s in subjects)
        {
            s.MakeUnemployed();
        }
    }

    private void OnValidate()
    {
        HashSet<Profession> requiredDefinitions = Enum.GetValues(typeof(Profession)).Cast<Profession>().ToHashSet();
        
        foreach (var definition in professionDefinitions)
        {
            if (requiredDefinitions.Contains(definition.typeFor)) requiredDefinitions.Remove(definition.typeFor);
        }

        if (requiredDefinitions.Count > 0)
        {
            Debug.LogError("Missing profession definitions in the Subject Manager for the following professions:", this);
            foreach (var definition in requiredDefinitions)
            {
                Debug.LogError(definition.ToString());
            }
            
            Debug.LogError("Either the definitions aren't serialized in the manager or the definitions themselves aren't properly configured (make sure each definition is set to the type it is for.)");
        }
    }

    public ProfessionDefinition GetDefinitionFor(Profession p)
    {
        foreach (var def in professionDefinitions)
        {
            if (def.typeFor == p) return def;
        }
        
        Debug.LogError($"No profession definition found for requested type {p.ToString()}.");
        return null;
    }

    /// <summary>
    /// Inform the subject manager that a new job has become available so that it can go into the queue and be filled by an unemployed worker as soon as possible.
    /// </summary>
    /// <param name="workspace">The location of the job that we wish to have a worker for.</param>
    public void RegisterJob(SubjectWorkspace workspace)
    {
        jobQueue.Enqueue(workspace); // job joins the back of the queue
        Debug.Log("Enqueuing a new job.");
        MatchUnemployedToJob();
    }

    /// <summary>
    /// Adds a new subject to the legion and triggers a search for a vacancy for them.
    /// </summary>
    public void CreateNewSubject()
    {
        LegionSubject subject = Instantiate(subjectPrefab, new Vector2(0f, 1f), Quaternion.identity, transform);
        subjects.Add(subject);
        MatchUnemployedToJob();
    }
    
    [ContextMenu("Force Employment Check")]
    private void MatchUnemployedToJob()
    {
        var unemployed = subjects.FindAll(subject => !subject.IsEmployed);

        while (jobQueue.Count > 0 && unemployed.Count > 0)
        {
            SubjectWorkspace workspace = jobQueue.Dequeue();
            LegionSubject match = unemployed.OrderBy(s =>
                Vector2.SqrMagnitude((Vector2)s.transform.position - (Vector2)workspace.transform.position)).First();
            unemployed.Remove(match);
            match.ChangeProfession(workspace);
        }
    }

    /// <summary>
    /// Compute the average morale, defined as the sum of morale of all workers divided by the number of workers.
    /// </summary>
    /// <returns>The average morale, a float between 0 (desertion) and 1 (perfect morale).</returns>
    public float GetAverageMorale()
    {
        float acc = 0f;

        foreach (var subject in subjects)
        {
            acc += subject.Morale;
        }

        return (subjects.Count > 0) ? acc / subjects.Count : 1f;
    }
}