using System;
using UnityEngine;

/// <summary>
/// Responsible for selecting pseudorandom background and foreground tiles, as well as decorative asset placement after the location of all zones has been determined.
/// </summary>

public class WorldGenerator : MonoBehaviour
{
    private static int _tileSpawnSalt = Pseudorandom.SaltOf("tile-spawn");
    
    [SerializeField] private GameObject[] wildernessGroundTiles;
    [SerializeField] private GameObject[] wildernessNearBackgroundTiles;
    [SerializeField] private GameObject[] wildernessMidBackgroundTiles;

    private Legion legion;
    
    private void Awake() => legion = GetComponentInParent<Legion>();

    private void OnEnable()
    {
        GameSession.WhenWorldReady(Generate); // when the world is ready - paint it!
    }

    private void Generate()
    {
        var zones = legion.ZoneManager.GetAllZones();
        
        Debug.Log(zones.Count);

        foreach (var z in zones)
        {
            // TODO: Make it type based, for now, we've got only wilderness assets so let's just use those everywhere
            // foreground
            PaintWithinBounds(wildernessGroundTiles, z.Type, z.Bounds);
            // near background
            PaintWithinBounds(wildernessNearBackgroundTiles, z.Type, z.Bounds);
            // mid background
            PaintWithinBounds(wildernessMidBackgroundTiles, z.Type, z.Bounds);
            // TODO: far background isn't drawn yet!
        }
    }

    private void PaintWithinBounds(GameObject[] tiles, ZoneType type, RectInt bounds)
    {
        int tileCount = bounds.width / GlobalConstants.ZoneWidthMultipleWorldUnits; // always an int because the zone manager only allows multiples of this constant

        for (int i = 0; i < tileCount; i++)
        {
            var pos = new Vector3(bounds.xMin + i * GlobalConstants.ZoneWidthMultipleWorldUnits,
                GlobalConstants.GroundY, 0f);

            int randomnessKey = bounds.xMin + i;
            GameObject tileHere = tiles[Pseudorandom.HashRange(0, tiles.Length, randomnessKey, _tileSpawnSalt)];
            
            Instantiate(tileHere, pos, Quaternion.identity, transform);
        }
    }
}
