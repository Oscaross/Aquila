using System;
using UnityEngine;

public class Farm : MonoBehaviour
{
      [Header("The maximum amount of grain that a farm building can hold before workers must start transporting it back to the base.")]
      [SerializeField] private float maxGrainBuffer;
      [SerializeField] private float currentGrainStore;

      public Action OnGrainStored;
      
      public void StoreGrain(float grain)
      {
            if (grain <= 0) return;
            
            Debug.Log($"Attempting to store {grain} grain.");
            float projected = currentGrainStore + grain;
            if (projected > maxGrainBuffer) Debug.Log("Grain store in this farm building is full.");

            currentGrainStore = Mathf.Min(projected, maxGrainBuffer);
            OnGrainStored.Invoke();
      }

      public void TransportGrain(float grain)
      {
            
      }
}