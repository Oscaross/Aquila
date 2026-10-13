using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the state of all clouds, ensures that they are performance-efficient and wrap around when out of view of the player. 
/// </summary>

public class CloudManager : MonoBehaviour
{
    // The wind speed is applied to clouds in the near band. The mid band and far band move according to the speed * the multiplier as defined below.
    
    [SerializeField] private List<CloudBand> bands;
    
    [SerializeField] private float windSpeed = 1f;
    [SerializeField] private Direction windDirection = Direction.Left;
    [SerializeField] private float midBandWindSpeedMultiplier = 0.9f;
    [SerializeField] private float farBandWindSpeedMultiplier = 0.5f;

    [SerializeField] private int nearCloudCount = 12;
    [SerializeField] private int midCloudCount = 8;
    [SerializeField] private int farCloudCount = 5;
    
    private void Start()
    {
        foreach (CloudBand band in bands)
        {
            switch (band.bandType)
            {
                case CloudType.Near:
                    band.SpawnClouds(nearCloudCount);
                    break;
                case CloudType.Mid:
                    band.SpawnClouds(midCloudCount);
                    break;
                case CloudType.Far:
                    band.SpawnClouds(farCloudCount);
                    break;
            }
        }
    }

    private void LateUpdate()
    {
        foreach (var band in bands)
        {
            band.MoveClouds(SpeedFor(band.bandType), windDirection);
        }
    }

    // Scale the speed by the speed scaling factor for this band so further clouds move at a slower rate than nearer bands.
    private float SpeedFor(CloudType t) => t switch
    {
        CloudType.Near => windSpeed,
        CloudType.Mid => windSpeed * midBandWindSpeedMultiplier,
        CloudType.Far => windSpeed * farBandWindSpeedMultiplier,
        _ => 0f,
    };
}