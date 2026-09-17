using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Sky/HorizonPreset")]
public class HorizonPreset : ScriptableObject
{
    [Tooltip("The colour that the horizon should go throughout this sunset/sunrise.")]
    public Gradient horizonColour;
    [Tooltip("How much the horizon should contribute to the haze. More dramatic colours tend to want lower bias because higher bias makes backgrounds more oddly coloured.")]
    public float hazeHorizonBias;
}