using System;
using System.Collections.Generic;
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

    [SerializeField, Range(0f, 1f)] private float twinkleProportion = 0.5f;
    [SerializeField] private float framesPerSecond = 10f;
    [SerializeField] private Vector2 rateVariation = new(0.8f, 1.2f);
    [SerializeField] private Vector2 holdTimeRange = new(1f, 5f);

    [SerializeField] private SpriteRenderer skyRenderer;

    private List<Star> stars = new();
    private float t;
    private Bounds sky;
    
    private void Awake()
    {
        sky = skyRenderer.bounds;
        RebuildStarfield();
    }

    private void LateUpdate()
    {
        foreach (Star s in stars)
        {
            s.UpdateStar(GameTime.Now);
        }
    }

    private void RebuildStarfield()
    {
        CreateStars(StarType.Weak, weakStarCount);
        CreateStars(StarType.Strong, strongStarCount);
    }

    private void CreateStars(StarType type, int numStars)
    {
        var choices = starsToChooseFrom.FindAll((s) => s.GetComponent<Star>().type == type);

        if (choices.Count <= 0)
        {
            Debug.LogError($"Tried to create stars for type {type} but no star prefabs of that type were found!", this);
            return;
        }
        
        for (int i = 0; i < numStars; i++)
        {
            // Get a position for this star by sampling a random Vector2 inside the SkySprite rectangular bounds
            float randomX = Random.Range(sky.min.x, sky.max.x);
            float randomY = Random.Range(sky.min.y, sky.max.y);

            Vector2 pos = new Vector2(randomX, randomY);

            float ppu = GlobalConstants.PixelsPerUnit; // we must place them in quantised positions on the sprite otherwise arbitrary pixels may be chosen by the renderer
            
            pos.x = Mathf.Round(pos.x * ppu) / ppu;
            pos.y = Mathf.Round(pos.y * ppu) / ppu;
            
            var starPrefab = choices[Random.Range(0, choices.Count)];
            
            Star star = Instantiate(starPrefab, pos, Quaternion.identity, transform);

            bool shouldTwinkle = Random.value < twinkleProportion;
            float fps = shouldTwinkle ? framesPerSecond * Random.Range(rateVariation.x, rateVariation.y) : 0f;
            float hold = Random.Range(holdTimeRange.x, holdTimeRange.y);

            star.Init(fps, hold);
            stars.Add(star);
        }
    }
}
