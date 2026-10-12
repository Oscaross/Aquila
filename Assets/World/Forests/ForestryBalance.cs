using UnityEngine;

[CreateAssetMenu(fileName = "ForestryBalance", menuName = "Scriptable Objects/ForestryBalance")]
public class ForestryBalance : ScriptableObject
{
    [Tooltip("A lumberjack normally deals 1 hit of damage per chop.")]
    [Min(1)] public int treeMaxHealth = 25;
    [Tooltip("How many seconds elapse from when the tree first spawns as a baby sapling to a fully fellable tree.")]
    [Min(0.5f)] public float secondsToFullyGrow = 1200f;
    [Tooltip("What proportion of the total capacity of a forest zone starts covered by trees at world gen.")]
    [Range(0f, 1f)] public float perForestCarryingCapacity = 0.4f;
    [Tooltip("No tree's centre can be closer than this value to another tree's centre.")]
    [Range(0.25f, 2f)] public float minTreeSeparationWorldUnits = 0.5f;
    
    public FellableTree treePrefab;
}
