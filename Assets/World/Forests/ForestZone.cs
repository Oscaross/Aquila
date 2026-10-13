using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ForestZone : Zone<ForestZoneModel>
{
    #region Boilerplate
    public override ZoneType Type => ZoneType.Forest;

    private EntityStore<FellableTreeData> TreeData => GameSession.Current.State.ZoneState.FellableTrees;
    
    protected override void OnBind(WorldContext context)
    {
        // load trees from save and reconstruct them
        foreach (var t in TreeData.Items.Where(data => !data.HasBeenFelled && data.ZoneID == Id))
        {
            SpawnTree(t, context);
        }
    }
    
    #endregion

    private List<FellableTree> treeInstances = new();

    private void CreateNewTree(FellableTreeData data, WorldContext context)
    {
        TreeData.Add(data);
        SpawnTree(data, context);
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
        var newTrees = ForestGenerator.GenerateForest(this, 0.5f, ForestPresets, GameSession.Current.Context.Balance.forestry);

        foreach (var t in newTrees)
        {
            CreateNewTree(t, GameSession.Current.Context);
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
    
}