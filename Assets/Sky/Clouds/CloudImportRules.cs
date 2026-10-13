#if UNITY_EDITOR
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

/// <summary>
/// Cloud-specific import settings. General sprite settings (PPU, point filter, pivot)
/// are handled by the main Aseprite postprocessor; this only adds what clouds need.
/// Applies to every .aseprite file in the same folder as this script (and its subfolders),
/// so moving the folder needs no code changes.
/// </summary>
public class CloudImportRules : AssetPostprocessor
{
    private static string cloudFolder;

    // Run after the general rules so nothing here gets overwritten.
    public override int GetPostprocessOrder() => 10;

    private void OnPreprocessAsset()
    {
        if (assetImporter is not AsepriteImporter ase) return;

        cloudFolder ??= ThisScriptsFolder();
        if (!assetPath.StartsWith(cloudFolder, StringComparison.Ordinal)) return;

        // Pixel colours are band labels (data), not display colours:
        // the shader compares them against hardcoded gamma-space keys.
        ase.sRGBTexture = false;

        ase.spriteMeshType = SpriteMeshType.Tight;

        // Only the sprite is used: CloudBand creates the GameObjects/SpriteRenderers at runtime,
        // so the importer's generated prefab is never used, and the art isn't animated.
        ase.generateModelPrefab = false;
        ase.generateAnimationClips = false;
        ase.addSortingGroup = false;
        ase.addShadowCasters = false;

        ase.mipmapEnabled = false;
    }

    // The compiler fills in this file's absolute path; convert it to "Assets/.../".
    private static string ThisScriptsFolder([CallerFilePath] string sourcePath = "")
    {
        string dataPath = Application.dataPath.Replace('\\', '/');              // ".../Project/Assets"
        string dir = Path.GetDirectoryName(sourcePath).Replace('\\', '/');      // ".../Project/Assets/.../Clouds"
        return "Assets" + dir.Substring(dataPath.Length) + "/";
    }
}
#endif