using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Shortcuts wrapping each sprite in a TileBase asset manually. We need a TileBase asset so that the procedural zone-based tile generator knows which tiles exist.
/// </summary>

public static class TileAssetCreator
{
    private const string OutputDir = "Assets/Ground/Tiles";

    [MenuItem("Assets/Create Tiles From Sprites")]
    private static void Create()
    {
        Sprite[] sprites = Selection.objects.OfType<Sprite>().ToArray();
        if (sprites.Length == 0)
        {
            Debug.LogWarning("No sprites selected. Select sliced sprites in the Project window.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OutputDir))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        int created = 0, skipped = 0;

        foreach (Sprite sprite in sprites)
        {
            string path = $"{OutputDir}/{sprite.name}.asset";

            if (AssetDatabase.LoadAssetAtPath<Tile>(path) != null)
            {
                skipped++;
                continue;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.flags = TileFlags.None;

            AssetDatabase.CreateAsset(tile, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Tile creation complete: {created} created, {skipped} skipped (already existed). " +
                  $"Output: {OutputDir}");
    }

    [MenuItem("Assets/Create Tiles From Sprites", true)]
    private static bool Validate() => Selection.objects.Any(o => o is Sprite);
}