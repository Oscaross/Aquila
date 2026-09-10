using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "ZoneTileset", menuName = "Scriptable Objects/ZoneTileset")]
public class ZoneTileset : ScriptableObject
{
    [System.Serializable]
    public struct ZoneTilesetVariant
    {
        [Tooltip("The 1wu * 1wu tile asset.")]
        public TileBase tile;
        [Tooltip("Can this tile be flipped on its z-axis to go from left-right instead of right-left and vice versa? Yes = more variation, No = less but we value the orientation of the tile.")]
        public bool allowMirror;
    }

    public ZoneType zone;
    [Tooltip("Tiles that should be placed inside a zone and do not form the border between different textures. Internal structure.")]
    public ZoneTilesetVariant[] middleTiles;
    [Tooltip("Tiles that form the left edge of a block of one texture and tail off into the dirt.")]
    public ZoneTilesetVariant[] leftEdgeTiles;
    [Tooltip("Tiles that form the right edge of a block of one texture and tail off into the dirt.")]
    public ZoneTilesetVariant[] rightEdgeTiles;
    [Tooltip("The proportion of these tiles that should be painted in a zone vs. no tiles. Density at 1 means no 'island' behaviour with clumps of tiles separated by raw dirt. 0 means no tiles at all.")]
    public int minIslandWidth;
    [Tooltip("An island is a contiguous block of tiles with no 'None' tiles between them. This variable DOES NOT matter at targetDensity = 1 as there are no islands.")]
    public int maxIslandWidth;
    [Tooltip("The minimum number of tiles that can occur between an island and its neighbour. This variable DOES NOT matter at targetDensity = 1 as there are no islands.")]
    public int minIslandSeparation;
    public int maxIslandSeparation;
}
