using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This component integrates with the custom pixel art shader by setting and computing light at a given world pos for a given source and then exposing the light levels to the LitShader; so that
/// lit objects correctly illuminate and tint when near a light source.
/// </summary>

[ExecuteAlways]
public class LightSource : MonoBehaviour
{
    public static readonly List<LightSource> Active = new List<LightSource>();

    public float radius = 6f;
    public float intensity = 1f;
    public LightTemperature temperature = LightTemperature.WARM;
    
    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();
}
