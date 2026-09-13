using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// This component integrates with the custom pixel art shader by setting and computing light at a given world pos for a given source and then exposing the light levels to the LitShader; so that
/// lit objects correctly illuminate and tint when near a light source.
/// </summary>

[ExecuteAlways]
public class LightSource : MonoBehaviour
{
    public static readonly List<LightSource> Active = new();

    [Tooltip("How far in world units the light source extends.")]
    public float radius = 6f;
    [Tooltip("The proportion of the maximum light intensity defined by the current weather that this light source can achieve. 1 for the maximum.")]
    public float maxIntensity = 1f;
    public LightTemperature temperature = LightTemperature.WARM;
    [Tooltip("The number of flicker cycles per second.")]
    public float flickerSpeed;
    [Tooltip("The fraction of the total intensity that the flicker deviates by. e.g. 0.3 would mean the source oscillates between 1 and 0.7 during each flicker cycle.")]
    public float flickerIntensity;
    [HideInInspector]
    public float flickerPhase; // prevent all of our light sources from pulsing at the same time
    
    private void OnEnable()
    {
        Active.Add(this);
        flickerPhase = Random.Range(0f, 8f);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();
}
