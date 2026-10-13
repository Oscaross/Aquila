using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Owns the state of all clouds, ensures that they are performance-efficient and wrap around when out of view of the player. 
/// </summary>

public class CloudManager : MonoBehaviour
{
    [SerializeField] private List<CloudBand> bands;
    
    [SerializeField] private float windSpeed = 1f;
    [SerializeField] private Direction windDirection = Direction.Left;

    private void Start()
    {
        foreach (CloudBand band in bands)
        {
            switch (band.bandType)
            {
                case CloudType.Near:
                    band.SpawnClouds(10);
                    break;
                case CloudType.Mid:
                    band.SpawnClouds(5);
                    break;
                case CloudType.Far:
                    band.SpawnClouds(15);
                    break;
            }
        }
    }

    private void LateUpdate()
    {
        foreach (var band in bands)
        {
            band.MoveClouds(windSpeed, windDirection);
        }
    }
}