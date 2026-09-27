using System;
using UnityEngine;

public enum Profession
{
    Unemployed,
    Farmer
}

public static class ProfessionExtensions
{
    /// <summary>
    /// Adds the correct component to a class for a given professsion, or throws if that behaviour doesn't exist in the codebase.
    /// </summary>
    /// <param name="p">The profession that we want the behaviour controller component class for.</param>
    /// <param name="go">The GameObject to assign the component to.</param>></para>
    /// <returns>The required component class as a Type, or null if none exists.</returns>
    public static ProfessionBehaviour AddBehaviour(this Profession p, GameObject go) => p switch
    {
        Profession.Unemployed => go.AddComponent<UnemployedBehaviour>(),
        Profession.Farmer => go.AddComponent<FarmerBehaviour>(),
        _ => throw new ArgumentOutOfRangeException(nameof(p), p, null)
    };
}