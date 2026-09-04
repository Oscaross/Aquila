using System;
using UnityEngine;

public class FarmField : MonoBehaviour
{
    [Header("Growth Logic")]
    
    [Tooltip("How grown up the current crops are on this field 0 = not grown, growthPhases.length - 1 = fully grown.")]
    [SerializeField] private int currentCropGrowthPhase;
    [Tooltip("The set of all sprites describing each distinct growth phase.")]
    [SerializeField] private Sprite[] growthPhases;
    [Tooltip("The state will be changed to wilting after this many growth cycles have passed without a farmer on the field.")]
    [SerializeField] private int phasesUntilWilting; 
    [Tooltip("The wilted sprite is shown after the field has been neglected and not worked for too long.")]
    [SerializeField] private Sprite wiltedSprite;
    [Tooltip("The current renderer of the current growth phase.")]
    [SerializeField] private SpriteRenderer currentGrowthPhaseRenderer;
    [Tooltip("The number of seconds (in game time) that must elapse before the crop growth phase increases by 1, or is harvested.")]
    [SerializeField] private float timeBetweenGrowthPhasesSeconds = 5f;
    
    [Header("Harvest Logic")] 
    [SerializeField] private float baseGrainYield = 10f;
    
    private int phasesSinceDesertion = 0;
    private bool isFieldDeserted = false;
    private float timeSinceLastPhaseSeconds;
    private SpriteRenderer fieldSprite;

    public Farm FarmParent
    {
        get;
        private set;
    }
    
    public Action<float> OnHarvestReady;
    public Vector2 FieldBoundsX
    {
        get
        {
            Bounds b = fieldSprite.bounds;
            return new Vector2(b.min.x, b.max.x);
        }
    }

    private void Awake()
    {
        // TODO: For now we'll just assume every field has a farmer so we can debug it
        fieldSprite = GetComponent<SpriteRenderer>(); 
        AssignFarmerToField();
    }

    /// <summary>
    /// True if a farmer is actively tending to this field (so no other farmer can), false otherwise.
    /// </summary>
    public bool IsFieldCurrentlyWorked
    {
        get;
        private set;
    }

    /// <summary>
    /// Attempts to assign a farmer to work this field. 
    /// </summary>
    /// <returns>True if the farmer is now working on the field and false if they couldn't be assigned, usually because another farmer occupies this field.</returns>
    public bool AssignFarmerToField()
    {
        if (IsFieldCurrentlyWorked) return false;

        timeSinceLastPhaseSeconds = 0f;

        IsFieldCurrentlyWorked = true;
        return true;
    }

    public void UnassignFarmerFromField()
    {
        IsFieldCurrentlyWorked = false;
        timeSinceLastPhaseSeconds = 0f;
    }


    public void Tick(float deltaGameSeconds)
    {
        timeSinceLastPhaseSeconds += deltaGameSeconds;
        
        while (timeSinceLastPhaseSeconds >= timeBetweenGrowthPhasesSeconds)
        {
            timeSinceLastPhaseSeconds -= timeBetweenGrowthPhasesSeconds;
            AdvancePhase();
        }
    }
    
    private void AdvancePhase()
    {
        if (isFieldDeserted) return;
        if (IsFieldCurrentlyWorked)
        {
            currentCropGrowthPhase++;
            
            if (currentCropGrowthPhase == growthPhases.Length)
            {
                MakeHarvestReady();
                return;
            }

            UpdateGrowthPhase(currentCropGrowthPhase);
            return;
        }

        phasesSinceDesertion++;
        if (phasesSinceDesertion >= phasesUntilWilting)
        {
            isFieldDeserted = true;
            currentGrowthPhaseRenderer.sprite = wiltedSprite;
        }
    }

    private void MakeHarvestReady()
    {
        UpdateGrowthPhase(0);
        OnHarvestReady?.Invoke(baseGrainYield);
    }

    private void UpdateGrowthPhase(int newPhase)
    {
        currentCropGrowthPhase = newPhase;
        currentGrowthPhaseRenderer.sprite = growthPhases[newPhase];
    }
}
