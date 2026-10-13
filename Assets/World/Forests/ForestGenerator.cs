using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ForestGenerator
{
    private static readonly int ForestPresetSalt = Pseudorandom.SaltOf("forest-preset-salt");
    private static readonly int TreeFillSalt = Pseudorandom.SaltOf("tree-fill-salt");
    private static readonly int TreeJitterSalt = Pseudorandom.SaltOf("tree-jitter-salt");
    private static readonly int TreeSpeciesTypeSalt = Pseudorandom.SaltOf("species-type-salt");
    private static readonly int TreeSpeciesVariantSalt = Pseudorandom.SaltOf("species-variant-salt");
    
    // We assume all trees are half a world unit wide. This doesn't really need to be precise, and it might vary slightly, but a mean value will suffice rather than loads of work.
    private const int TreeWidthPx = 8;

    /// <summary>
    /// A "jittered" slot-based algorithm:
    /// 1. Breaks the zone into equal width slots based on the amount of space a tree will occupy and how far it can be shifted left/right.
    /// 2. Fills a slot based on the fill chance. A higher fill chance means more slots are filled => more trees exist.
    /// 3. Randomly shifts the tree left/right about this slot, to produce some clumping/random behaviour.
    /// </summary>
    /// <param name="forest">The forest zone we want to generate trees for.</param>
    /// <param name="fillChance">The probability that a slot will be filled with a tree. Higher => more trees in total.</param>
    /// <param name="presets">The mix of different species desired i.e. 0.8 oak and 0.2 birch.</param>
    /// <param name="balance">Balancing constants such as tree width or minimum separation.</param>
    /// <returns>An array of generated tree position data ready for instantiating/saving.</returns>
    public static FellableTreeData[] GenerateForest(ForestZone forest, float fillChance,
        TreeSpeciesTypeWeight[][] presets, ForestryBalance balance)
    {
        if (presets.Length == 0)
        {
            Debug.LogError("Attempted to generate a forest with no species presets!");
            return Array.Empty<FellableTreeData>();
        }

        var species = ContentRegistry.All<TreeSpecies>().ToList();
        var preset = presets[Pseudorandom.HashRange(0, presets.Length - 1, forest.Id, ForestPresetSalt)];

        int ppu = GlobalConstants.PixelsPerUnit;
        int sep = balance.minTreeSeparationPx;
        int jitter = sep; // how far a tree may wander inside its slot 
        int slotW = TreeWidthPx + sep + jitter; // width of each tree slot
        int startPx = forest.Bounds.xMin * ppu;
        int widthPx = forest.Bounds.width * ppu;
        int slots = Mathf.Max(0,
            (widthPx - jitter - TreeWidthPx) / slotW +
            1); // the last tree must end inside the zone, this is how many available slots there are for trees in this zone

        var trees = new List<FellableTreeData>();
        for (int i = 0; i < slots; i++)
        {
            int key = Pseudorandom.Combine(i, forest.Id); // the two things that determine each time if a tree did or didn't spawn here => zone ID and the slot we are looking at
            // roll the "pseudorandom dice" for a pre-determined outcome of a tree spawning or not spawning
            if (Pseudorandom.HashRange(0, 9999, key, TreeFillSalt) >= fillChance * 10000) continue;

            int x = startPx + i * slotW + Pseudorandom.HashRange(0, jitter, key, TreeJitterSalt);

            var type = Pseudorandom.RoulettePicker(preset, w => w.Weight, key, TreeSpeciesTypeSalt).Type;
            string speciesKey = PickSpecies(species, type, key);
            if (speciesKey == null) continue;
            
            trees.Add(new FellableTreeData(balance.treeMaxHealth, x, speciesKey, forest.Id));
        }

        return trees.ToArray();
    }

    private static string PickSpecies(List<TreeSpecies> all, TreeSpeciesType type, int k)
    {
        var ofType = all.Where(t => t.type == type).OrderBy(t => t.Key, StringComparer.Ordinal).ToList(); // we MUST order by key since dictionaries don't return in a stable, deterministic order
        if (ofType.Count == 0)
        {
            Debug.LogError($"No tree species of type {type} in the content registry!");
            return null;
        }
        return ofType[Pseudorandom.HashRange(0, ofType.Count - 1, k, TreeSpeciesVariantSalt)].Key;
    }
}
