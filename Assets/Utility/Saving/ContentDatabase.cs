using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry containing all static and game-specific (rather than save-specific) objects such as ScriptableObjects.
/// Many features depend on information that is sourced from this global, save-agnostic repository such as tree species, config data, building data, sprites, etc...
/// </summary>

public static class ContentRegistry
{
    private static Dictionary<string, ContentAsset> _keyedAssets;

    /// <summary>
    /// Attempt to fetch a ScriptableObject (resource) by its key.
    /// </summary>
    /// <param name="key">The unique identifier key for this ScriptableObject (category:name) format.</param>
    /// <typeparam name="T">A ContentAsset or derivative of ContentAsset that is stored in the registry.</typeparam>
    /// <returns>The desired ScriptableObject instance.</returns>
    /// <exception cref="KeyNotFoundException">Throws if no such ContentAsset exists in the registry. Check the name is exactly correct and that the registry is successfully being rebuilt.</exception>
    public static T Get<T>(string key) where T : ContentAsset
    {
        _keyedAssets ??= Build(); // we only rebuild the keyed assets file if our working dictionary is null
        if (_keyedAssets.TryGetValue(key, out ContentAsset asset) && asset is T typed) return typed;
        throw new KeyNotFoundException($"No {typeof(T).Name} with key {key} found!");
    }

    private static Dictionary<string, ContentAsset> Build()
    {
        var dict = new Dictionary<string, ContentAsset>();
        foreach (ContentAsset a in Resources.LoadAll<ContentAsset>("Content"))
        {
            if (!dict.TryAdd(a.Key, a))
                throw new InvalidOperationException($"Duplicate content key found at key {a.Key}!");
        }

        return dict;
    }
    
    // This makes sure that when we're adding new ScriptableObjects the registry recaches each time we recompile and updates with the new key
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => _keyedAssets = null;
    
}