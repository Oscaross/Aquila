// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using Random = UnityEngine.Random;
//
// public class ForestZone : Zone, IFindable, IEntity<ForestZoneModel>
// {
//     public int Id { get; }
//     public override ZoneType Type => ZoneType.Forest;
//     
//     [Header("WorldGen Logic")]
//     [SerializeField] private FellableTree[] fellableTreePrefabs;
//     [Tooltip("The maximum tree density this forest zone is able to sustain. When a forest zone is first created, this is the density that it occupies. The absolute max number of trees is the integer width of this zone multiplied by this proportion.")]
//     [SerializeField] private float carryingCapacity;
//     [Tooltip("The rate at which the forest moves towards the carrying capacity.")]
//     [SerializeField] private float growthRate;
//     [SerializeField] private int maxClumpSize = 10;
//
//     private List<FellableTree> fellableTrees = new();
//     private ForestZoneModel data;
//
//     private void FixedUpdate()
//     {
//         // Allows trees to grow by ticking each tree in this zone each frame
//         foreach (FellableTree t in fellableTrees)
//         {
//             t.Tick();
//         }
//     }
//
//     public void Bind(ForestZoneModel dataModel)
//     {
//         data = dataModel;
//         GameClock.OnSunrise += OnNewDay;
//         
//         // Calculate the number of initial trees we wish to spawn and spawn them
//         int fellableTreesToSpawn = Mathf.RoundToInt(carryingCapacity * (Bounds.xMax - Bounds.xMin));
//
//         for (int i = 0; i < fellableTreesToSpawn; i++)
//         {
//             FellableTree t = Instantiate(PickTreeVariant(), transform.position, Quaternion.identity, transform);
//             data.fellableTrees.Add(t);
//         }
//     }
//
//     public override void OnZoneDestroyed()
//     {
//         GameClock.OnSunrise -= OnNewDay;
//     }
//
//     private void OnNewDay()
//     {
//         foreach (FellableTreeData f in data.fellableTrees)
//         {
//             if (f.hasBeenFelled)
//             {
//                 data.fellableTrees.Remove(f);
//             }
//         }
//     }
//     
//     
//     // TODO: Make seeded and maybe some logic by where in the zone we're picking from?
//     private FellableTree PickTreeVariant() => fellableTreePrefabs[Random.Range(0, fellableTreePrefabs.Length)];
//
//     private int GetTreeSpawnXPos()
//     {
//         // 1. Pick a random tree, weighted by trees with the most neighbours. The most likely tree for us to pick is the one that's the closest to the most trees
//         Dictionary<FellableTree, int> neighbours = new Dictionary<FellableTree, int>();
//
//         int sum = 0;
//         
//         // foreach (FellableTree t in data.fellableTrees)
//         // {
//         //     // All the trees within a 5 world unit radius of this
//         //     int count = data.fellableTrees.FindAll(a => a.transform.position.x - t.transform.position.x < 5f).Count;
//         //     neighbours.Add(t, count);
//         //
//         //     sum += count;
//         // }
//         
//         // Imagine a line going from i = 0 to the sum of all neighbour counts for each tree. Pick a random number between 0 and sum, whichever tree that lands on we choose
//
//         // int chosen = Pseudorandom.HashRange(0, sum, transform.position.GetHashCode(), Pseudorandom.TreeSpawnSalt);
//
//         FellableTree origin;
//
//         int curr = 0;
//         
//         foreach ((FellableTree t, int count) in neighbours)
//         {
//             curr += count;
//         }
//         
//  
//         
//         // 2. For this tree, pick a spawn position that's weighted towards being in the closest possible x slot to this tree. 
//
//         return 1;
//     }
// }
//
// [Serializable]
// public class ForestZoneModel : ISaveRecord
// {
//     public List<FellableTreeData> fellableTrees;
// }
