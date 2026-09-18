using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages all stars in the night sky including making them twinkle, randomising their position and ensuring they are in the correct density and location each night and that they fade in and out according to the day-night cycle.
/// </summary>

public class StarField : MonoBehaviour
{
    [SerializeField] private List<Star> starsToChooseFrom;
    [SerializeField] private int weakStarCount;
    [SerializeField] private int strongStarCount;
    [SerializeField] private int giantStarCount;

    [SerializeField, Range(0f, 1f)] private float twinkleProportion = 0.5f;
    [SerializeField] private float framesPerSecond = 10f;
    [SerializeField] private Vector2 rateVariation = new(0.8f, 1.2f);
    [SerializeField] private Vector2 holdTimeRange = new(1f, 5f);

    [SerializeField] private Vector2 rotationCentre;
    [Tooltip("The field rotates throughout the day night cycle, resetting at noon so it doesn't visibly snap.")]
    [SerializeField] private float degreesPerDayNightCycle = 15f;

    [SerializeField] private SpriteRenderer skyRenderer;

    private List<Star> stars = new();
    private Vector2[] basePositions;
    private Bounds sky;

    private const int MaxSpawnAttemptsPerStar = 30;
    
    private void Awake()
    {
        sky = skyRenderer.bounds;
        // Order matters here, we want the largest stars to get the best positions first.
        CreateStars(StarType.Giant, giantStarCount);
        CreateStars(StarType.Strong, strongStarCount);
        CreateStars(StarType.Weak, weakStarCount);

        // Store the original positions so that we don't rotate our stars and deform them as the star field rotates in the night
        basePositions = new Vector2[stars.Count];

        for (int i = 0; i < stars.Count; i++)
        {
            var star = stars.ElementAt(i);
            basePositions[i] = new Vector2(star.transform.localPosition.x, star.transform.localPosition.y);
        }
    }

    private void LateUpdate()
    {
        float a = GameClock.SinceNoon * degreesPerDayNightCycle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(a), cos = Mathf.Cos(a);
        float ppu = GlobalConstants.PixelsPerUnit;
        
        // rotate star field about the pivot
        for (int i = 0; i < basePositions.Length; i++)
        {
            Vector2 d = basePositions[i] - rotationCentre;
            Vector2 p = rotationCentre + new Vector2(d.x * cos - d.y * sin, d.x * sin + d.y * cos);
            
            stars[i].transform.localPosition = new Vector3(Mathf.Round(p.x * ppu) / ppu,
                Mathf.Round(p.y * ppu) / ppu,
                stars[i].transform.localPosition.z);
            
            stars[i].UpdateStar(GameClock.ElapsedSeconds);
        }
    }

    private void CreateStars(StarType type, int numStars)
    {
        var choices = starsToChooseFrom.FindAll(s => s.type == type);

        if (choices.Count <= 0)
        {
            Debug.LogError($"Tried to create stars for type {type} but no star prefabs of that type were found!", this);
            return;
        }
        
        for (int i = 0; i < numStars; i++)
        {
            var starPrefab = choices[Random.Range(0, choices.Count)];
            
            if (!GenerateStarPos(starPrefab, out Vector2 pos))
            {
                Debug.LogWarning($"StarField: placed {i} of {numStars} {type} stars before running out of room. " +
                                 $"Reduce the count or the separation radius.", this);
                break;
            }
            
            Star star = Instantiate(starPrefab, pos, Quaternion.identity, transform);

            bool shouldTwinkle = (type == StarType.Giant) || (Random.value < twinkleProportion);
            float fps = shouldTwinkle ? framesPerSecond * Random.Range(rateVariation.x, rateVariation.y) : 0f;
            float hold = Random.Range(holdTimeRange.x, holdTimeRange.y);

            star.Init(fps, hold);
            stars.Add(star);
        }
    }

    private bool GenerateStarPos(Star starPrefab, out Vector2 spawnPos)
    {
        float ppu = GlobalConstants.PixelsPerUnit;
        float minSeparation = starPrefab.minSeparationWorldUnits;

        for (int i = 0; i < MaxSpawnAttemptsPerStar; i++)
        {
            // Quantise to the pixel grid it will be placed on, rather than relying on the camera to move it slightly.
            Vector2 candidate = new(Mathf.Round(SampleX() * ppu) / ppu,
                Mathf.Round(SampleY() * ppu) / ppu);

            float clumpMultiplier = Random.Range(0.7f, 1.3f); // create natural clumping behaviour
            bool canSpawn = true;

            foreach (Star s in stars)
            {
                float threshold = Mathf.Max(s.minSeparationWorldUnits, minSeparation) * clumpMultiplier;

                // Squared magnitude to avoid expensive sqrt() calls.
                if (((Vector2)s.transform.position - candidate).sqrMagnitude < threshold * threshold)
                {
                    canSpawn = false;
                    break;
                }
            }

            if (canSpawn)
            {
                spawnPos = candidate;
                return true;
            }
        }

        spawnPos = default;
        return false;
    }

    private float SampleX() => Random.Range(sky.min.x, sky.max.x);
    // bias stars to spawn more towards the higher y values (further from the horizon).
    private float SampleY() => Mathf.Lerp(sky.min.y, sky.max.y, Mathf.Sqrt(Random.value));
}
