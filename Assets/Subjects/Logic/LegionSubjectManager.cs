using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Responsible for assigning workers to job sites when the demand presents itself.
/// </summary>

public class LegionSubjectManager : MonoBehaviour
{
    [SerializeField] private LegionSubject baseSubjectPrefab;
    [SerializeField] private List<LegionSubject> subjects = new();
    private Queue<SubjectWorkspace> jobQueue = new();

    private void Awake()
    {
        // TODO: Debug we just make a couple of guys for different professions at the origin
        for (int i = 0; i < 10; i++)
        {
            CreateNewSubject();
        }
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
        LegionSubject subject = Instantiate(baseSubjectPrefab, new Vector2(0f, 1f), Quaternion.identity, transform);
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