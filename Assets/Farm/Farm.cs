using System;
using UnityEngine;

public class Farm : MonoBehaviour
{
      [Header("The maximum amount of grain that a farm building can hold before workers must start transporting it back to the base.")]
      [SerializeField] private float maxGrainBuffer;
      [SerializeField] private float currentGrainStore;

      public Action OnGrainStoreFull;
      
      public void StoreGrain(float grain)
      {
            Debug.Log($"Attempting to store {grain} grain.");
            float projected = currentGrainStore + grain;
            if (projected > maxGrainBuffer) OnGrainStoreFull.Invoke();

            currentGrainStore = Mathf.Min(projected, maxGrainBuffer);
      }

      public void OnboardFarmer(Farmer farmer)
      {
            
      }
}