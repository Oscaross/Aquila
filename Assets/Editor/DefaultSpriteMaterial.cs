using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically adds the "LitSpriteMaterial" mat to any imported asset so we don't have to do it each time.
/// </summary>

[InitializeOnLoad]
static class DefaultSpriteMaterial
{
    const string MatPath = "Assets/Shaders/Illumination/LitSpriteMaterial.mat";

    static DefaultSpriteMaterial() => ObjectFactory.componentWasAdded += OnAdded;

    static void OnAdded(Component c)
    {
        if (c is not SpriteRenderer sr) return;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat) sr.sharedMaterial = mat;
        else
        {
            Debug.LogError("Failed to load the LitSpriteMaterial to auto-assign to a newly imported asset! Did you change the path for the LitSpriteMaterial.mat file?" +
                           "To fix ensure the file is at path Assets/Shaders/Illumination/LitSpriteMaterial.mat or change the expected path in DefaultSpriteMaterial.cs");
        }
    }
}