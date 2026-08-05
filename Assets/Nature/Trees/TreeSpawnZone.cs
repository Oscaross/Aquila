using System.Collections.Generic;
using UnityEngine;

/**
 * Manages a single spawn zone for trees, including where new ones spawn, which trees spawn and how many are allowed to spawn.
*/

public class TreeSpawnZone : MonoBehaviour
{
    [SerializeField] private float width = 20f;
    [SerializeField] private float minDistanceBetweenTrees = 0.5f;
    [SerializeField] private FunctionalTree treePrefab;
    [SerializeField] private List<FunctionalTree> treesInZone = new();

    /// <summary>
    /// The maximum density a zone can sustain.
    /// </summary>
    public float CarryingCapacity;
    /// <summary>
    /// The rate at which the forest moves towards the carrying capacity. Higher growth rate means rapid deforestation carries a lower penalty.
    /// </summary>
    public float GrowthRate;
    /// <summary>
    /// When the player first generates the save, what should the occupancy of the forest be.
    /// </summary>
    public float InitialOccupancy;

    public float XMin => transform.position.x - width * 0.5f;
    public float XMax => transform.position.x + width * 0.5f;
    public float CurrentDensity => treesInZone.Count / width;
    public float Occupancy => CarryingCapacity > 0f ? CurrentDensity / CarryingCapacity : 1f;
    public IReadOnlyList<FunctionalTree> TreesInZone => treesInZone;

    public void AddTree(FunctionalTree t) => treesInZone.Add(t);
    public void RemoveTree(FunctionalTree t) => treesInZone.Remove(t);

    public bool TryGetSpawnPosition(out float x, int attempts = 10)
    {
        x = 0f;

        for (int i = 0; i < attempts; i++)
        {
            x = Random.Range(XMin, XMax);
            Debug.Log($"Sampled spawn pos at {x}");
            if (CanSpawnAt(x)) return true;
        }
        x = 0f;
        return false;
    }

    // TODO: Make this return from many possible prefabs based on species density in this zone etc.
    public FunctionalTree PickSpecies()
    {
        return treePrefab;
    }

    public bool CanSpawnAt(float x)
    {
        if (x < XMin || x > XMax) return false;
        foreach (var t in treesInZone)
            if (Mathf.Abs(t.transform.position.x - x) < minDistanceBetweenTrees) return false;
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(XMin, transform.position.y, 0),
                        new Vector3(XMax, transform.position.y, 0));
    }
}