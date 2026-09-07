using System;
using UnityEngine;

public class FarmField : SubjectWorkspace
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
    
    
    public Action<float> OnHarvestReady;

    public override Profession RequiredProfession => Profession.Farmer;


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
        if (IsCurrentlyWorked)
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
