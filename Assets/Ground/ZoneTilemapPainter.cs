using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

/// <summary>
/// Fills the entire map with tiles, setting each tile within a zone to have its zone-specific tile and wilderness to have wilderness-specific tiles. 
/// </summary>

public class ZoneTilemapPainter : MonoBehaviour
{
    [Tooltip("The unity tilemap object that we are painting over. Should be only one in the scene.")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private ZoneManager zoneManager;
    [Tooltip("Each individual tile palette for each individual zone.")]
    [SerializeField] private ZoneTileset[] tilesets;
    [Tooltip("The seed that this save uses. The seed is the value that means we can reconstruct the exact same tilemap 'randomness' regardless of how many times we must reconstruct it.")]
    [SerializeField] private int seed;

    private void Start()
    {
        PaintTilemap();
    }

    /// <summary>
    /// Iterates over all zones within the tilemap and fills them with the corresponding zone-specific tile.
    /// </summary>
    [ContextMenu("Paint Tilemap")]
    public void PaintTilemap()
    {
        if (tilemap == null || zoneManager == null)
        {
            Debug.LogError("The tilemap painter is missing a reference to either the tilemap (Unity's implementation of how a tilemap works) or the zone manager (how the painter knows what zone is where).");
            return;
        }

        tilemap.ClearAllTiles();

        RectInt globalArea = zoneManager.worldBounds;
        int tileCount = globalArea.width;

        var positions = new Vector3Int[tileCount];
        var tiles = new TileBase[tileCount];

        var zonesInWorld = zoneManager.GetAllZones(); // this is ORDERED in ascending order
        int idx = 0;

        foreach (var zone in zonesInWorld)
        {
            var types = GenerateTilePositionTypes(GetTilesetOf(zone.type), zone.bounds.width, idx + zoneManager.worldBounds.xMin);
            
            // For this zone, iterate over each x coordinate and make a corresponding position index and fetch the tile
            foreach (var posType in types)
            {
                int worldX = zoneManager.worldBounds.xMin + idx;
                
                positions[idx] = new Vector3Int(worldX, 0);
                var tile = PickVariant(GetTilesetOf(zone.type), posType, worldX);
                tiles[idx] = tile;

                idx++;
            }
            
        }
        
        tilemap.SetTiles(positions, tiles);
    }

    private ZoneTileset GetTilesetOf(ZoneType type)
    {
        foreach (var tileset in tilesets)
        {
            if (tileset.zone == type) return tileset;
        }

        return null;
    }

    private TilePositionType?[] GenerateTilePositionTypes(ZoneTileset set, int zoneSize, int runStartWorldX)
    {
        var positions = new TilePositionType?[zoneSize];

        int idx = 0;
        bool isGeneratingIsland = (Pseudorandom.Hash01(set.GetHashCode(), Pseudorandom.TilePainterIslandSalt) > 0.5); // sometimes we start with island, other times we start with gap otherwise zones always start with one or the other
        int currIslandWidth = 0;
        int currGapWidth = 0;
        int gapWidthTarget = GenerateRandomGapSize(set, runStartWorldX);
        int islandWidthTarget = GenerateRandomIslandSize(set, runStartWorldX);
        
        while (idx < zoneSize)
        {
            if (isGeneratingIsland)
            {
                positions[idx] = TilePositionType.Middle;
                idx++;
                currIslandWidth++;
                if (currIslandWidth == islandWidthTarget)
                {
                    currIslandWidth = 0;
                    gapWidthTarget = GenerateRandomGapSize(set, runStartWorldX + idx);
                    isGeneratingIsland = false;
                }
            }
            else
            {
                positions[idx] = null;
                idx++;
                currGapWidth++;

                if (currGapWidth == gapWidthTarget)
                {
                    isGeneratingIsland = true;
                    currGapWidth = 0;
                    islandWidthTarget = GenerateRandomIslandSize(set, runStartWorldX + idx);
                }
            }
        }
        
        // Now we've populated the tilemap and generated the islands, go back over and set the edge tiles to left or right edges as required.
        for (int i = 0; i < positions.Length; i++)
        {
            if (positions[i] == null) continue; // no change needed on a none tile

            bool solidLeft = i > 0 && positions[i - 1] != null; // a tile is a left most tile if its tile to the left is none
            bool solidRight = i < positions.Length - 1 && positions[i + 1] != null;

            positions[i] = (solidLeft, solidRight) switch
            {
                (true, true) => TilePositionType.Middle, // a left and a right neighbour must be in the middle
                (false, true) => TilePositionType.LeftEdge,
                (true, false) => TilePositionType.RightEdge,
                (false, false) => TilePositionType.Middle
            };
        }

        return positions;
    }

    private int GenerateRandomIslandSize(ZoneTileset set, int runStartWorldX) =>
        Pseudorandom.HashRange(set.minIslandWidth, set.maxIslandWidth + 1,
            runStartWorldX, Pseudorandom.TilePainterIslandSalt);

    private int GenerateRandomGapSize(ZoneTileset set, int runStartWorldX) =>
        Pseudorandom.HashRange(set.minIslandSeparation, set.maxIslandSeparation + 1,
            runStartWorldX, Pseudorandom.TilePainterGapSalt);

    /// <summary>
    /// Pseudorandom. Uses very large arbitrary values to provide deterministic randomness. This uses the seed which is stored on this class, meaning any save with the same seed always generates the same random tiles.
    /// </summary>
    /// <param name="set">The set of tiles that we can pick from.</param>
    /// <param name="type">Whether this is a middle, left edge, right edge or null tile.</param>
    /// <param name="worldX">The x coordinate of the tile we're generating for in WORLD coordinates.</param>
    /// <returns>The tile that belongs at this particular x coordinate, null if we want a gap (no tile at this coordinate).</returns>
    private TileBase PickVariant(ZoneTileset set, TilePositionType? type, int worldX)
    {
        uint h = (uint)(worldX * 374761393 + (int)set.zone * 668265263 + seed * 2654435761u);
        h = (h ^ (h >> 13)) * 1274126177;
        h ^= h >> 16;
        
        var tileset = type switch
        {
            TilePositionType.Middle => set.middleTiles,
            TilePositionType.LeftEdge => set.leftEdgeTiles,
            TilePositionType.RightEdge => set.rightEdgeTiles,
            _ => null
        };

        if (tileset == null) return null;
        
        return tileset[h % (uint)tileset.Length].tile;
    }
}

/// <summary>
/// Used to generate a more complex "island" type patchy set of tiles, rather than a continuous block. "None" means no tile is generated, and instead the dirt below will show which gives internal contrast in zones.
/// </summary>
enum TilePositionType
{
    LeftEdge,
    Middle,
    RightEdge
}