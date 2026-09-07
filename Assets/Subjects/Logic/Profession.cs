using System;

public enum Profession
{
    Unemployed,
    Farmer,
    Lumberjack,
    Archer,
}

public static class ProfessionExtensions
{
    /// <summary>
    /// Exposes the correct component class given a profession.
    /// </summary>
    /// <param name="p">The profession that we want the behaviour controller component class for.</param>
    /// <returns>The required component class as a Type, or null if none exists.</returns>
    public static Type TypeFor(Profession p) => p switch
    {
        Profession.Unemployed => typeof(UnemployedBehaviour),
        Profession.Farmer => typeof(FarmerBehaviour),
        Profession.Lumberjack => typeof(Lumberjack),
        _ => null
    };
}