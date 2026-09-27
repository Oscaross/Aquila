using UnityEngine;

/// <summary>
/// Authored data for one profession: which behaviour it maps to, how it looks, and its tuning values.
/// Behaviours read from this; nothing profession-specific is serialised on the behaviour itself.
/// </summary>
[CreateAssetMenu(fileName = "ProfessionDefinition", menuName = "Scriptable Objects/Subjects/ProfessionDefinition")]
public class ProfessionDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("The profession this asset defines. Also used as the stable key in save files.")]
    public Profession typeFor;

    [Header("Visuals")] 
    public bool hasOverlay = true;
    [Tooltip("Overrides the base controller's clips: work actions, and unemployed's own locomotion.")]
    public RuntimeAnimatorController animations;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Behaviour")]
    public float idleSeconds = 6f;
}