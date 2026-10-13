using System;
using Newtonsoft.Json;
using UnityEngine;

public class FellableTree : MonoBehaviour, IEntity<FellableTreeData>, IFindable
{
    [Tooltip("The leaves (i.e. to apply a shader to during the seasons).")]
    private SpriteRenderer leaves;
    [Tooltip("The trunk and the branches, aka the wooden part of the tree that is NOT the stump that gets left after chopping.")]
    private SpriteRenderer trunk;
    [Tooltip("The stump that gets left behind after the tree is felled.")]
    private SpriteRenderer stump;
    [Tooltip("A single sprite that gets rendered, when all the others are false and we have a sapling.")]
    private SpriteRenderer sapling;
    
    public int Id { get; }
    
    private FellableTreeData data;

    private int TicksPerGrowthPhase =>
        Ticks.FromSeconds(GameSession.Current.Context.Balance.forestry.secondsToFullyGrow / data.Species.saplings.Length);

    private int currentGrowthPhase;
    
    public void Bind(FellableTreeData model, WorldContext save)
    {
        data = model;
        ResolveSprites();
    }

    public void Refresh(long currentTick)
    {
        long tickDelta = currentTick - data.PlantedAtTick;
        
        Debug.Log("Ticks elapsed since planting: " + tickDelta);
        currentGrowthPhase = Mathf.FloorToInt((float) tickDelta / TicksPerGrowthPhase);
        currentGrowthPhase = Mathf.Clamp(currentGrowthPhase, 0, data.Species.saplings.Length); // we can only display from sapling phase 1 to the max sapling phase from the species
        ResolveSprites();
        
    }

    private void ResolveSprites()
    {
        var species = ContentRegistry.Get<TreeSpecies>(data.SpeciesID);

        if (species == null)
        {
            Debug.LogError("FellableTree has no species object so it does not know how to render itself!", this);
            return;
        }
        
        // Growth phase needs to be less than saplings.Length if it's a non-fully grown sapling 
        if (currentGrowthPhase < data.Species.saplings.Length)
        {
            leaves.enabled = false;
            trunk.enabled = false;
            stump.enabled = false;
            sapling.enabled = true;

            sapling.sprite = data.Species.saplings[currentGrowthPhase];
        }
        else
        {
            leaves.sprite = species.leaves;
            trunk.sprite = species.trunk;
            stump.sprite = species.stump;

            sapling.enabled = false;
            trunk.enabled = true;
            leaves.enabled = true;
            stump.enabled = true;
        }
    }
}

public class FellableTreeData : EntityData
{
    [JsonConstructor]
    public FellableTreeData() {}
    
    public FellableTreeData(int maxHealth, int xPosPx, string speciesID, int zoneID)
    {
        Health = maxHealth;
        XPosPx = xPosPx;
        SpeciesID = speciesID;
        ZoneID = zoneID;
    }
    
    public int Health;
    public int ZoneID;
    public long PlantedAtTick;
    public int XPosPx;
    public bool HasBeenFelled;
    public string SpeciesID;

    [JsonIgnore] public TreeSpecies Species => ContentRegistry.Get<TreeSpecies>(SpeciesID);
}