using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ForestGenerator
{
    private static float MinSeparationBetweenTrees => GameSession.Current.Context.Balance.forestry.minTreeSeparationWorldUnits;
    private static List<TreeSpecies> AllSpeciesAssets => ContentRegistry.All<TreeSpecies>().ToList();
    private static List<int> assignedPositions = new();
    
    public static readonly int TreeSpawnPosSalt = Pseudorandom.SaltOf("tree-spawn-pos");
    public static readonly int TreeSpeciesSalt = Pseudorandom.SaltOf("tree-species");
    
    // We assume all trees are half a world unit wide. This doesn't really need to be precise, and it might vary slightly, but a mean value will suffice rather than loads of work.
    private const int TreeWidthPx = 8;
    private const int MaxIterations = 1000;
    
    public static FellableTreeData[] GenerateForest(ForestZone forest, float carryingCapacity, TreeSpeciesTypeWeight[][] speciesDensities)
    {
        int treeWidthsThatFit = Mathf.RoundToInt(forest.Bounds.width / (float) TreeWidthPx);
        int numTrees = Mathf.RoundToInt((treeWidthsThatFit - 1) * MinSeparationBetweenTrees * carryingCapacity); // n - 1 intervals between each tree at LEAST, scaled by the carrying capacity

        var generatedTrees = new List<FellableTreeData>();
        
        // Pick a random forest configuration (i.e. 5/6 oak and 1/6 birch)
        if (speciesDensities.Length == 0)
        {
            Debug.LogError("Attempted to generate a forest with no species density presets!");
            return null;
        }
        
        var forestConfiguration =
            speciesDensities[Pseudorandom.HashRange(0, speciesDensities.Length - 1, forest.Id, TreeSpeciesSalt)];

        int iters = 0;
        
        while (CurrentTreeDensity(forest) < carryingCapacity && iters < MaxIterations)
        {
            iters++;

            if (!TryGenerateTreeX(forest.Bounds.xMin, forest.Bounds.xMax, assignedPositions.Count, forest.Id, out int xPosPx)) continue;
            
            assignedPositions.Add(xPosPx);
            
            // Pick a random species from our forest configuration. The species must depend on the x position of this tree AND the ID of its parent forest for stable determinism
            var speciesType = Pseudorandom.RoulettePicker(forestConfiguration, t => t.Weight, Pseudorandom.Combine(xPosPx, forest.Id), TreeSpeciesSalt);
            string speciesID = GetRandomTreeSpeciesID(xPosPx, forest.Id, speciesType.Type);

            generatedTrees.Add(new FellableTreeData(GameSession.Current.Context.Balance.forestry.treeMaxHealth, xPosPx, speciesID));
        }

        assignedPositions = new(); // we've generated this forest's data dump so reset the cached position list
        return generatedTrees.ToArray();
    }

    /// <summary>
    /// Defined as the number of trees currently in the forest divided by the number of available "slots" for trees (ref MinSeparationBetweenTrees).
    /// </summary>
    private static float CurrentTreeDensity(ForestZone forest) => assignedPositions.Count / (forest.Bounds.width * GlobalConstants.PixelsPerUnit / (float) TreeWidthPx);
    
    private static string GetRandomTreeSpeciesID(int xPosPx, int zoneID, TreeSpeciesType t)
    {
        var treesOfType = AllSpeciesAssets.FindAll(a => a.type == t);

        if (treesOfType.Count == 0) return treesOfType[Pseudorandom.HashRange(0, treesOfType.Count - 1, Pseudorandom.Combine(xPosPx, zoneID), TreeSpawnPosSalt)].Key;
 
        Debug.Log($"No trees of type {t.ToString()} were found to return from the content registry!");
        return null;
    }
    
    /// <summary>
    /// Algorithm to take the existing tree positions in the forest, the minimum distance between trees and the bounds that tries to find a suitable, realistic spot for a new tree to go.
    /// Works by:
    /// 1. Identifying "clusters" of trees  
    /// </summary>
    private static bool TryGenerateTreeX(int xMin, int xMax, int treesSpawned, int zoneID, out int xPosPx)
    {
        xPosPx = Pseudorandom.HashRange(16 * xMin, 16 * xMax, Pseudorandom.Combine(treesSpawned, zoneID),
            TreeSpawnPosSalt);
        return true;
    }
}
