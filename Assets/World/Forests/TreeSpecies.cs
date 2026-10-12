using UnityEngine;

[CreateAssetMenu(fileName = "TreeSpecies", menuName = "Scriptable Objects/Forests/TreeSpecies")]
public class TreeSpecies : ContentAsset
{
    public TreeSpeciesType type;
    public Sprite trunk;
    public Sprite[] saplings;
    public Sprite leaves;
    public Sprite stump;
}
