using System.Collections.Generic;
using UnityEngine;

/**
 * Manages the resource inventory for a given legion. 
*/
public class LegionResources : MonoBehaviour
{
    [SerializeField] private int[] resourceCount = new int[System.Enum.GetValues(typeof(Resource)).Length];

    /// <summary>
    /// Fetches the count of a given resource.
    /// </summary>
    /// <param name="resource">The resource to query (e.g. grain).</param>
    /// <returns>The count of that resource this legion currently has.</returns>
    public int GetResourceCount(Resource r) => resourceCount[(int) r];

    public bool ConsumeResource(Resource r, int howMuch)
    {
        if (!CanConsumeResource(r, howMuch)) return false;

        resourceCount[(int) r] -= howMuch;
        return true;
    }

    public bool CanConsumeResource(Resource r, int howMuch)
    {
        return GetResourceCount(r) >= howMuch;
    }

    public void AddResource(Resource r, int howMuch)
    {
        resourceCount[(int)r] += howMuch;
    }
}
