// using System;
// using UnityEngine;
//
// public class FellableTree : MonoBehaviour, IFindable, IEntity<FellableTreeData>
// {
//     public int Id => data.id;
//     private FellableTreeData data;
//
//     private int ticksPerPhase;
//
//     [SerializeField] private int secondsPerGrowthPhase;
//     [SerializeField] private Shaker shaker;
//     [SerializeField] private SpriteRenderer stump;
//     [SerializeField] private SpriteRenderer branches;
//     [SerializeField] private SpriteRenderer leaves;
//     [SerializeField] private SpriteRenderer sapling;
//     
//     public bool HasBeenFelled => data.health < 1;
//     public bool IsMature => Phase == data.species.saplings.Length;
//     public int Phase => Mathf.Min(data.ticksElapsedSincePlanted / ticksPerPhase, data.species.saplings.Length);
//     
//     private readonly int shownPhase = -1; // what our sprites currently show
//
//     public void Bind(FellableTreeData dataModel)
//     {
//         data = dataModel;
//         ticksPerPhase = Mathf.Max(1, Mathf.RoundToInt(secondsPerGrowthPhase / Time.fixedDeltaTime)); // we consider a tick to be the number of FixedUpdate calls required to elapse secondsPerGrowthPhase
//         ResolveGrowthPhase();
//     }
//
//     public void Tick()
//     {
//         if (HasBeenFelled || IsMature) return; // don't need to grow a fully mature/cut down tree
//
//         data.ticksElapsedSincePlanted++;
//         if (Phase != shownPhase) ResolveGrowthPhase();
//     }
//
//     public void Chop()
//     {
//         if (HasBeenFelled) return;
//         
//         data.health--;
//         shaker.Shake();
//
//         if (HasBeenFelled)
//         {
//             Fell();
//         }
//     }
//
//     /// <summary>
//     /// Configures the sprite renderers to show the tree's correct state depending on what stage in the growth cycle it's at.
//     /// </summary>
//     private void ResolveGrowthPhase()
//     {
//         if (IsMature)
//         {
//             // Sapling off, everything else on
//             sapling.enabled = false;
//             branches.enabled = true;
//             branches.sprite = data.species.branches;
//             leaves.enabled = true;
//             leaves.sprite = data.species.leaves;
//             stump.enabled = true;
//             stump.sprite = data.species.stump;
//         }
//         else
//         {
//             // Sapling to its growth phase and everything non-sapling off
//             sapling.sprite = data.species.saplings[Phase];
//             sapling.enabled = true;
//             branches.enabled = false;
//             leaves.enabled = false;
//             stump.enabled = false;
//         }
//     }
//
//     private void Fell()
//     {
//         // TODO: Spawn a work package, a pallette of chopped wood to be taken back.
//     }
// }
//
// [Serializable]
// public class FellableTreeData : ISaveRecord
// {
//     public int id; 
//     public int health;
//     public int ticksElapsedSincePlanted;
//     public int xPos;
//     public bool hasBeenFelled;
// }