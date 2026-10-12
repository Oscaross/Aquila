using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ForestZone : Zone<ForestZoneModel>
{
    #region Boilerplate
    public override ZoneType Type => ZoneType.Forest;
    
    protected override void OnBind(WorldContext context)
    {
        allSpeciesAssets = ContentRegistry.All<TreeSpecies>().ToList();
        
        foreach (var t in Model.FellableTrees)
        {
            SpawnTree(t, context);
        }
    }
    
    #endregion

    private List<FellableTree> treeInstances = new();
    // Simply a list of the available tree species and their properties we can choose from.
    private List<TreeSpecies> allSpeciesAssets;

    private void CreateNewTree(int xPos, WorldContext context)
    {
        var data = new FellableTreeData(context.Balance.forestry.treeMaxHealth, xPos, GetRandomTreeSpeciesID(xPos, TreeSpeciesType.Birch));
        SpawnTree(data, context);
    }

    private string GetRandomTreeSpeciesID(int xPos, TreeSpeciesType t)
    {
        var treesOfType = allSpeciesAssets.FindAll(a => a.type == t);

        if (treesOfType.Count == 0) return treesOfType[Pseudorandom.HashRange(0, treesOfType.Count - 1, xPos, ForestGenerator.TreeSpawnPosSalt)].Key;
 
        Debug.Log($"No trees of type {t.ToString()} were found to return from the content registry!", this);
        return null;
    }

    private void SpawnTree(FellableTreeData tree, WorldContext context)
    {
        // The x position is stored in pixels, not world units so we must convert back into world units when we put into world space
        var instance = Instantiate(GameSession.Current.Context.Balance.forestry.treePrefab, new Vector3(tree.XPosPx / (float) GlobalConstants.PixelsPerUnit, 0f, 0f), Quaternion.identity, transform);
        
        treeInstances.Add(instance);
        context.Registry.Register(instance);
    }

    #region Worldgen

    /// <summary>
    /// Defines the aesthetics of each forest, giving a mix of trees in different forests with certain weightings.
    /// </summary>
    private static readonly TreeSpeciesTypeWeight[][] ForestPresets =
    {
        new[] { new TreeSpeciesTypeWeight(TreeSpeciesType.Oak, 4), new TreeSpeciesTypeWeight(TreeSpeciesType.Birch, 2) },
        new[] { new TreeSpeciesTypeWeight(TreeSpeciesType.Birch, 1), new TreeSpeciesTypeWeight(TreeSpeciesType.Oak, 3) },
        new[] { new TreeSpeciesTypeWeight(TreeSpeciesType.Pine, 5), new TreeSpeciesTypeWeight(TreeSpeciesType.Birch, 1) },
    };

    public override void OnZoneCreated()
    {
        var newTrees = ForestGenerator.GenerateForest(this, 0.4f, ForestPresets);

        foreach (var t in newTrees)
        {
            GameSession.Current.State.ZoneState.FellableTrees.Add(t); // make a record of this tree so it persists
            SpawnTree(t, GameSession.Current.Context);
        }
    }

    #endregion

    private void FixedUpdate()
    {
        // Every 1 real world second, trigger an update to all trees
        var currTick = GameSession.Current.TicksElapsed;
        
        if (currTick % Ticks.PerSecond == 0)
        {
            foreach (var t in treeInstances) t.Refresh(currTick);
        }
    }
}

public class ForestZoneModel : ZoneModel
{
    public List<FellableTreeData> FellableTrees = new();
}