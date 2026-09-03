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


    private float timeSinceLastGrowthPhaseSeconds = 0f;
    private int phasesSinceDesertion = 0;
    private bool isFieldDeserted = false;

    private void Awake()
    {
        // TODO: For now we'll just assume every field has a farmer so we can debug it
        AssignFarmerToField();
    }

    public bool IsFieldCurrentlyWorked
    {
        get;
        private set;
    }

    public bool AssignFarmerToField()
    {
        if (IsFieldCurrentlyWorked) return false;

        IsFieldCurrentlyWorked = true;
        return true;
    }

    private void LateUpdate()
    {
        timeSinceLastGrowthPhaseSeconds += Time.deltaTime * TimeOfDay.Instance.TimeMultiplier;

        if (timeSinceLastGrowthPhaseSeconds >= timeBetweenGrowthPhasesSeconds)
        {
            timeSinceLastGrowthPhaseSeconds = 0;
            Grow();
        }
    }

    private void Grow()
    {
        if (isFieldDeserted) return;
        if (IsFieldCurrentlyWorked)
        {
            currentCropGrowthPhase++;
            
            if (currentCropGrowthPhase == growthPhases.Length)
            {
                Harvest();
                return;
            }
            
            currentGrowthPhaseRenderer.sprite = growthPhases[currentCropGrowthPhase];
            return;
        }

        phasesSinceDesertion++;
        if (phasesSinceDesertion >= phasesUntilWilting)
        {
            isFieldDeserted = true;
            currentGrowthPhaseRenderer.sprite = wiltedSprite;
        }
    }

    private void Harvest()
    {
        Debug.Log("Harvest time");
        currentCropGrowthPhase = 0;
    }
}
