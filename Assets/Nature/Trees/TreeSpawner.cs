using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/**
 * Maintains forest sizes and growth/destruction of trees in spawn zones.
*/

public class TreeSpawner : MonoBehaviour
{
    [SerializeField] private List<TreeSpawnZone> spawnZones;
    [SerializeField] private int maxTreeSpawnAttempts;

    private void OnEnable()
    {
        TimeOfDay.OnSunrise += this.OnNewDay;
    }

    private void OnDisable()
    {
        TimeOfDay.OnSunrise -= this.OnNewDay;
    }

    private void Start()
    {
        GenerateNewForest(); // give us some initial forests
    }

    /// <summary>
    /// Returns references to each tree within the target bounds.
    /// <param name="xMin">The minimum (left) x bound (inclusive).</param>
    /// <param name="xMax">The maximum (right) x bound (inclusive).</param>
    /// <returns>A list of references to each tree found within the target bounds.</returns>
    /// </summary>
    public List<FunctionalTree> GetTreesInRange(float xMin, float xMax)
    {
        var result = new List<FunctionalTree>();
        foreach (var zone in spawnZones)
        {
            if (zone.XMax < xMin || zone.XMin > xMax) continue; // zone can't overlap, skip it
            foreach (var t in zone.TreesInZone)
                if (t.transform.position.x >= xMin && t.transform.position.x <= xMax)
                    result.Add(t);
        }
        return result;
    }

    public List<FunctionalTree> GetChoppableTreesInRange(float xMin, float xMax) => GetTreesInRange(xMin, xMax).Where(t => t != null && t.CanTarget).ToList();

    private void GenerateNewForest()
    {
        foreach (var zone in spawnZones)
        {
            int guard = 0;
            while (zone.Occupancy < zone.InitialOccupancy && guard < 500)
            {
                if (!TrySpawn(zone)) break;   // can't place any more, stop
                guard++;
            }
        }
    }

    private void OnNewDay()
    {
        if (GameTime.CurrentSeason == Season.Winter) return;

        foreach (var zone in spawnZones)
        {
            float occupancy = zone.Occupancy;
            if (Random.value >= zone.GrowthRate * occupancy * (1f - occupancy)) continue;

            TrySpawn(zone);
        }
    }

    private bool TrySpawn(TreeSpawnZone zone)
    {
        if (zone.TryGetSpawnPosition(out float x, maxTreeSpawnAttempts))
        {
            var tree = Instantiate(zone.PickSpecies(), new Vector2(x, zone.transform.position.y),
                                   Quaternion.identity, zone.transform);
            zone.AddTree(tree);

            return true;
        }

        return false;
    }
}