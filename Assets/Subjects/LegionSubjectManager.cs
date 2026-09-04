using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for assigning workers to job sites when the demand presents itself.
/// </summary>

public class LegionSubjectManager : MonoBehaviour
{
    [Tooltip("A prefab asset for each type of subject i.e. lumberjack, archer, farmer, etc...")] 
    [SerializeField] private LegionSubject[] subjectPrefabs;
    private List<LegionSubject> subjects = new();
    private List<Job> jobs = new();

    private void Awake()
    {
        // TODO: Debug we just make a couple of guys for different professions at the origin
        for (int i = 0; i < subjectPrefabs.Length; i++)
        {
            LegionSubject subject = Instantiate(subjectPrefabs[i], new Vector2(0f, 1f), Quaternion.identity, transform);
            subjects.Add(subject);
        }
    }
    
    private void FillJobs()
    {
        foreach (var subject in subjects)
        {
            if (subject.IsEmployed) continue;
            
            
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

        return (subjects.Count > 0) ? 1 : acc / subjects.Count;
    }

    /// <summary>
    /// 
    /// </summary>
    public void PostJob()
    {
        
    }
}