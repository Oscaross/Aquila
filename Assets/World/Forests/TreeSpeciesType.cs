public enum TreeSpeciesType
{
    Birch,
    Oak,
    Poplar,
    Pine
}

// Small helper that lets us define a weighting for how many of a type of tree should spawn in a forest
public struct TreeSpeciesTypeWeight
{
    public TreeSpeciesTypeWeight(TreeSpeciesType treeSpeciesType, int weight)
    {
        Type = treeSpeciesType;
        Weight = weight;
    }
    
    public TreeSpeciesType Type;
    public int Weight;
}