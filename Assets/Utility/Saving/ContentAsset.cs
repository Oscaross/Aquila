using UnityEngine;

/// <summary>
/// The template for any ScriptableObject that must be loadable as a resource by features of the game.
/// All ScriptableObjects must use this class instead, as they inherit the ScriptableObject class as a base plus an identifiable key that can be used by the registry to expose SOs at runtime.
/// </summary>

public abstract class ContentAsset : ScriptableObject
{
    [SerializeField] private string key;
    /// The unique identifier of this ContentAsset. Convention is "category:name" so that identical keys in different areas of the world don't collide, for example "npcs:farmer" or "sounds:farmer". 
    public string Key => key;
}
