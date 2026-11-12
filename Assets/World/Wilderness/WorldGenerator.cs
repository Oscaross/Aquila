using UnityEngine;

/// <summary>
/// Responsible for selecting pseudorandom background and foreground tiles, as well as decorative asset placement after the location of all zones has been determined.
/// </summary>

public class WorldGenerator : MonoBehaviour
{
    private static readonly int TileSpawnSalt = Pseudorandom.SaltOf("tile-spawn");
    
    [SerializeField] private GameObject[] wildernessGroundTiles;
    [SerializeField] private GameObject[] wildernessNearBackgroundTiles;
    [SerializeField] private GameObject[] wildernessMidBackgroundTiles;

    private static readonly int HazeID = Shader.PropertyToID("_Haze");
    private static MaterialPropertyBlock _block;

    private Legion legion;
    private BackgroundGenerator generator;
    
    private void Awake() 
    {
        legion = GetComponentInParent<Legion>();
        generator = GetComponent<BackgroundGenerator>();
        
        _block = new();
    }

    private void OnEnable()
    {
        GameSession.WhenWorldReady(Generate); // when the world is ready - paint it!
    }

    private void Generate()
    {
        // Spawn the epic background for the main level (index = 0)
        generator.SpawnEpicBackground(0);
        
        var zones = legion.ZoneManager.GetAllZones();

        // 1. Spawning backgrounds
        foreach (var z in zones)
        {
            generator.SpawnBackground(z, 0);
        }
        
        // TODO: CUT!
        foreach (var z in zones)
        {
            // TODO: Make it type based, for now, we've got only wilderness assets so let's just use those everywhere
            // foreground
            PaintWithinBounds(wildernessGroundTiles, TileLayer.Ground, z.Bounds);
            // near background
            PaintWithinBounds(wildernessNearBackgroundTiles, TileLayer.NearBackground, z.Bounds);
            // mid background
            PaintWithinBounds(wildernessMidBackgroundTiles, TileLayer.MidBackground, z.Bounds);
            // TODO: far background isn't drawn yet!
        }
    }

    private void PaintWithinBounds(GameObject[] tiles, TileLayer type, RectInt bounds)
    {
        int tileCount = bounds.width / GlobalConstants.ZoneWidthMultipleWorldUnits; // always an int because the zone manager only allows multiples of this constant
        GameObject go = null;

        for (int i = 0; i < tileCount; i++)
        {
            var pos = new Vector3(bounds.xMin + i * GlobalConstants.ZoneWidthMultipleWorldUnits,
                GlobalConstants.GroundY, 0f);

            int randomnessKey = bounds.xMin + i;
            GameObject tileHere = tiles[Pseudorandom.HashRange(0, tiles.Length, randomnessKey, TileSpawnSalt)];
            
            go = Instantiate(tileHere, pos, Quaternion.identity, transform);
        }
        
        // Add a haze material, we don't need one if this is a ground tile
        if (type != TileLayer.Ground && go != null)
        {
            var rend = go.GetComponent<Renderer>();

            if (rend == null)
            {
                Debug.LogError($"Non-foreground tile at pos {go.transform.position} requires haze, but doesn't have a Renderer so we can't add haze to it.");
                return;
            }
            
            ApplyHaze(rend, type);
        }
    }

    private void ApplyHaze(Renderer r, TileLayer t)
    {
        r.GetPropertyBlock(_block);
        _block.SetFloat(HazeID, t.GetAttributes().Haze);
        r.SetPropertyBlock(_block);
    }
}