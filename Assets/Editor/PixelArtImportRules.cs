using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

/// <summary>
/// Ensures that all assets that are imported into the system have the correct import settings. Flags assets that fail important checks like having the right PPU
/// </summary>

public class PixelArtImportRules : AssetPostprocessor
{
    // Only police game art; adjust the path to taste.
    bool IsGameArt => assetPath.StartsWith("Assets/") && !assetPath.Contains("/UI/");

    void OnPreprocessTexture()
    {
        if (!IsGameArt) return;
        var importer = (TextureImporter)assetImporter;
        if (importer.textureType != TextureImporterType.Sprite) return;

        importer.spritePixelsPerUnit = GlobalConstants.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        // Single-sprite textures: force the pivot directly.
        if (importer.spriteImportMode == SpriteImportMode.Single)
        {
            var s = new TextureImporterSettings();
            importer.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            s.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(s);
        }
    }
    
    /// <summary>
    /// This is the importer for aseprite files that ensures they are also read correctly.
    /// </summary>
    void OnPreprocessAsset()
    {
        if (assetImporter is not AsepriteImporter ase) return;

        ase.pivotSpace = PivotSpaces.Local;
        ase.pivotAlignment = SpriteAlignment.BottomLeft;
        ase.spritePixelsPerUnit = GlobalConstants.PixelsPerUnit;
        ase.filterMode = FilterMode.Point;
    }

    // Sliced sheets: validate every sprite after import.
    void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
    {
        if (!IsGameArt) return;
        foreach (var sprite in sprites)
        {
            // ignore warning on IDE here - exact equality is correct because a slight mismatch here will cause very obscure rendering bugs to hide in the project
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (sprite.pixelsPerUnit != GlobalConstants.PixelsPerUnit) Debug.LogError($"Sprite '{sprite.name}' in {assetPath} has been imported at the wrong PPU {sprite.pixelsPerUnit}.");
            
            Vector2 p = sprite.pivot; // already in pixels
            bool whole = Mathf.Approximately(p.x, Mathf.Round(p.x)) &&
                         Mathf.Approximately(p.y, Mathf.Round(p.y));
            if (!whole)
                Debug.LogError($"Sprite '{sprite.name}' in {assetPath} has non-integer pivot {p}px.",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath));
        }
    }
}