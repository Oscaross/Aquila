using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// The component responsible for animating subjects and activating different sprite sheets based on which profession they hold.
/// </summary>

public class SubjectVisuals : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Tooltip("The overlay that's profession specific such as a farmer overlay having the hat and scythe or lumberjack having an axe and cap.")]
    [SerializeField] private SpriteRenderer overlay;

    private Gait currentGait;

    // Gait is the movement animation flag that tells our sprite how to animate while moving. It is a continuous animation that loops until we change Gait.
    private static readonly int GaitHash = Animator.StringToHash("Gait");
    // DoWork is a trigger flag that tells our sprite to play a single animation of the profession-specific work animation.
    private static readonly int DoWorkHash = Animator.StringToHash("DoWork");
    private const string OverlayLayerName = "Overlay";
    
    public void Apply(ProfessionDefinition def)
    {
        if (def.animations == null)
        {
            Debug.LogError($"{def.name} has no animations assigned.", def);
            return;
        }

        // Assigning a controller resets layer weights and parameters, so it must come first.
        animator.runtimeAnimatorController = def.animations;

        overlay.enabled = def.hasOverlay;
        Debug.Log("Has overlay? " + overlay.enabled);
        int overlayLayer = animator.GetLayerIndex(OverlayLayerName);
        if (overlayLayer < 0) Debug.LogError($"No '{OverlayLayerName}' layer in {def.animations.name}.", this);
        else animator.SetLayerWeight(overlayLayer, def.hasOverlay ? 1f : 0f);

        animator.SetInteger(GaitHash, (int)currentGait); // restore what was playing before the swap
    }

    public void RefreshAnimations(Gait gait)
    {
        currentGait = gait;
        animator.SetInteger(GaitHash, (int)gait);
    }

    public void TriggerWorkAnimation() => animator.SetTrigger(DoWorkHash);
}
