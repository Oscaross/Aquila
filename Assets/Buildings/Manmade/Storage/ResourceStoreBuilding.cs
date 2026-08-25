using UnityEngine;

/**
 * Controls the logic for a resource store building, subscribing to events and rendering the current diagetic state of that resource store.  
*/

public class ResourceStoreBuilding : MonoBehaviour
{
    [Tooltip("Every sprite (in order) that shows the diagetic state of the player's resource building. As idx grows, the drawn state is a larger store (i.e. more grain in the barn).")]
    [SerializeField] private Sprite[] diageticStates;
    [Tooltip("The current diagetic state based on what percentage of the stores are currently saturated with that resource, set by the class as it subscribes to the legion resource manager.")]
    [SerializeField] private SpriteRenderer currentDiageticState;
    [SerializeField] private Resource resource;

    public Resource Resource => resource;

    private Legion legion;
    private LegionResources resources;

    private void Awake()
    {
        legion = GetComponentInParent<Legion>();
        resources = legion.Resources;
    }

    private void Start()
    {
        OnResourceCountChange(resource); // prevent stale sprite before real change
    }

    private void OnEnable()
    {
        resources.ResourceChangeOccurred += OnResourceCountChange;
    }

    private void OnDisable()
    {
        resources.ResourceChangeOccurred -= OnResourceCountChange;
    }

    private void OnResourceCountChange(Resource r)
    {
        if (resource != r) return; // our resource isn't the one that changed - done

        int numStates = diageticStates.Length;

        float fraction = Mathf.Clamp01(resources.GetResourceCount(r) / (float)resources.GetResourceCapacity(r));
        int idx = Mathf.Clamp(
            Mathf.FloorToInt(fraction * diageticStates.Length),
            0, diageticStates.Length - 1); // the index is the "percentage" full our stores are multiplied by the number of diagetic states, clamped between the array's valid index bounds

        currentDiageticState.sprite = diageticStates[idx];
    }
}
