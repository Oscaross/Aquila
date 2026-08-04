using System.Collections.Generic;
using UnityEngine;

/**
 * Manages the resource inventory for a given legion. 
*/
public class LegionResources : MonoBehaviour
{
    [SerializeField] private int[] resourceCount = new int[System.Enum.GetValues(typeof(Resource)).Length]; // map of all resource counts the player currently owns in this legion
    [SerializeField] private int[] resourceCapacity = new int[System.Enum.GetValues(typeof(Resource)).Length]; // map of all max resource capacities in this legion
    [Tooltip("The minimum percentage change in a resource count to cause a diagetic UI alert to the player.")]
    [SerializeField] private float significantResourceChangeThreshold = 0.05f;

    public event System.Action<Resource> SignificantResourceChangeOccurred;
    public event System.Action<Resource> ResourceChangeOccurred;

    private void Awake()
    {
        // The resource array serialises when this is created, so without this code we'd have to manually reset the component every time the resource system changes.
        int required = System.Enum.GetValues(typeof(Resource)).Length;
        if (resourceCount == null || resourceCount.Length != required || resourceCapacity.Length != required)
        {
            System.Array.Resize(ref resourceCount, required);
            System.Array.Resize(ref resourceCapacity, required);
        } 

        foreach (int c in resourceCapacity)
        {
            if (c == 0) Debug.LogWarning("A resource cap is set to zero for some legion. This will not allow the player to accumulate resources.");
        }
    }

    /// <summary>
    /// Fetches the count of a given resource.
    /// </summary>
    /// <param name="resource">The resource to query (e.g. grain).</param>
    /// <returns>The count of that resource this legion currently has.</returns>
    public int GetResourceCount(Resource r) => resourceCount[(int) r];

    /// <summary>
    /// Uses the resource.
    /// </summary>
    /// <param name="r">The resource.</param>
    /// <param name="howMuch">How much as an integer.</param>
    /// <returns>True if the resource was consumed, false if there was an issue (i.e. the player doesn't have enough of it).</returns>
    public bool ConsumeResource(Resource r, int howMuch)
    {
        if (!CanConsumeResource(r, howMuch)) return false;

        ResourceChangeOccurred?.Invoke(r);
        CheckSignificantChange(r, resourceCount[(int)r], howMuch);
        resourceCount[(int) r] -= howMuch;
        return true;
    }

    public bool CanConsumeResource(Resource r, int howMuch)
    {
        return GetResourceCount(r) >= howMuch;
    }

    /// <summary>
    /// Adds a resource to the player's resource stores. If the maximum capacity is exceeded, the resource is added and consumed but clamped at the max. 
    /// Imagine the player has 10 space and adds 15 resources, all 15 resources are consumed but the player only gains 10, taking them to the maximum capacity.
    /// </summary>
    /// <param name="r">The resource.</param>
    /// <param name="howMuch">How much as an integer.</param>
    /// <returns>True if the resource was added, false if the player's storage is full.</returns>
    public bool AddResource(Resource r, int howMuch)
    {
        int idx = (int)r;
        int space = resourceCapacity[idx] - resourceCount[idx];
        if (space <= 0) return false;

        int added = Mathf.Min(howMuch, space);
        int before = resourceCount[idx];
        resourceCount[idx] += added;

        CheckSignificantChange(r, before, added);
        ResourceChangeOccurred?.Invoke(r);
        return true;
    }

    public int GetResourceCapacity(Resource r) => resourceCapacity[(int) r];

    private void CheckSignificantChange(Resource r, int originalCount, int addedCount)
    {
        // This will cause div by zero if we progress. If we ever go from 0 of a resource and gain some, we should always notify the player.
        if (originalCount == 0) SignificantResourceChangeOccurred?.Invoke(r); 

        float delta = (float) addedCount / originalCount; // percentage change in resource count

        if (delta >= significantResourceChangeThreshold)
        {
            SignificantResourceChangeOccurred?.Invoke(r);
            Debug.Log("Significant resource change detected.");
        }

    }
}
