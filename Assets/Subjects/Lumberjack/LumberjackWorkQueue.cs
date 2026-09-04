using NUnit.Framework;
using System.Linq;
using UnityEngine;

public class LumberjackWorkQueue : MonoBehaviour
{
    public TreeSpawner treeSpawner;

    public bool TryGetJob(Vector2 from, out FunctionalTree job)
    {
        var candidates = treeSpawner.GetChoppableTreesInRange(-1000f, 1000f);
        job = candidates.OrderBy(t => Mathf.Abs(t.transform.position.x - from.x)).FirstOrDefault();
        return job != null;
    }
}
