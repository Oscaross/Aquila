using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class BackgroundGenerator : MonoBehaviour
{
    [SerializeField] private int epicBackgroundLayer = -100;
    [SerializeField] private int farBackgroundLayer = -75;
    [SerializeField] private int midBackgroundLayer = -50;
    [SerializeField] private int closeBackgroundLayer = -25;
    
    private static readonly int EpicBackgroundSalt = Pseudorandom.SaltOf("epic-background-salt");
    
    [SerializeField] private GameObject[] epicBackgrounds;

    /// <summary>
    /// Chooses the epic background for this level.
    /// </summary>
    /// <param name="levelIndex">The index of the current level the player is in. Offshoots are levels past index 0, which is the main level where the legion is located.</param>
    public void SpawnEpicBackground(int levelIndex)
    {
        if (epicBackgrounds.Length <= 0)
        {
            Debug.LogError("No epic backgrounds have been configured in the World game object!");
            return;
        }
        
        int selectedIdx = Pseudorandom.HashRange(0, epicBackgrounds.Length, levelIndex, EpicBackgroundSalt);
        
        // N.B. we want the camera to be the parent so the background follows the player around
        var bg = Instantiate(epicBackgrounds[selectedIdx], new Vector3(-19.5f, 3f, 0f), Quaternion.identity, Camera.main.transform);
        // TODO: Does our background even want parallax at this scale?
        // var parallax = bg.AddComponent<ParallaxLayer>();

        bg.layer = LayerMask.NameToLayer("Background");
        var sr = bg.GetComponent<SpriteRenderer>();
        
        sr.sortingLayerName = "Background";
        sr.sortingOrder = epicBackgroundLayer;

        bg.name = "EpicBackground";
    }


    public void SpawnBackground(Zone forZone, int levelIndex)
    {
        foreach (TileLayer tileLayer in Enum.GetValues(typeof(TileLayer)))
        {
            var attrs = tileLayer.GetAttributes();
            // var xPos = 
        }
    }
}
