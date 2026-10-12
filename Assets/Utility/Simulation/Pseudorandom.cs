using System;
using System.Collections.Generic;

/// <summary>
/// When you want deterministic random behaviour. Deterministic random behaviour ("pseudorandomness") is where outcomes/generation appears random but it is directly tied to a seed.
/// The seed is stored on Pseudorandom, anything that generates random values from Pseudorandom will always generate the same "random" outcome if the seed stays the same and the input arguments stay the same.
/// Mainly used in things that need to be reproduced/regenerated during saving.
///
/// Salts are also stored in this class. A salt is just a way to make the random generator give different, uncorrelated answers for different classes. Define a new static readonly salt when
/// we need two classes to ensure they pick random values differently, or reuse one if doing multiple random calls within the same system/system subset.
/// </summary>

public static class Pseudorandom
{
    /// <summary>
    /// Stable, deterministic salter that produces the same salt each run by given names. NEVER use string.GetHashCode(), only use this. We must be consistent, otherwise save files will break.
    /// N.B. this approach is called FNV-1a
    /// </summary>
    /// <param name="name">The (unique) name for the salt, usually just describe what it is doing with hyphens between it i.e. "tree-spawn-pos".</param>
    /// <returns>An integer salt.</returns>
    public static int SaltOf(string name)
    {
        uint h = 2166136261; // offset basis, so even the empty string has a non-trivial hash
        foreach (char c in name)
        {
            h ^= c; // this is an XOR
            h *= 16777619; // the FNV prime, that spreads bits across the whole 32-bit value
        }

        return (int)h; // overflow here is fine, because a wraparound just gives us a different salt but is always consistent with "name".
    }

    /// <summary>
    /// Returns a pseudorandom value between 0 and 1, analogous to the Random.value function.
    /// </summary>
    /// <param name="a">The first value to determine the outcome, such as the position of an object.</param>
    /// <param name="salt">The second value to determine the outcome, usually we want a salt here that stays the same for all random outcomes of this nature.</param>
    /// <returns>A pseudorandom floating point number between 0 and 1.</returns>
    public static float Hash01(int a, int salt) => HashUint(a, salt) / 4294967296f;

    /// <summary>
    /// Returns a pseudorandom integer between min (inclusive) and max (exclusive).
    /// </summary>
    /// <param name="min">The minimum (inclusive) integer this could be.</param>
    /// <param name="max">The maximum (exclusive) integer this could be.</param>
    /// <param name="a">The first value to determine the outcome, such as the position of an object.</param>
    /// <param name="salt">The second value to determine the outcome, usually we want a salt here that stays the same for all random outcomes of this nature.</param>
    /// <returns>A psuedorandom integer between min (inclusive) and max (exclusive).</returns>
    public static int HashRange(int min, int max, int a, int salt)
        => min + (int)(HashUint(a, salt) % (uint)(max - min));
    
    private static uint HashUint(int a, int salt)
    {
        uint h = (uint)(a * 374761393 + salt * 668265263 + GameSession.Current.State.Seed * 2654435761u);
        h = (h ^ (h >> 13)) * 1274126177;
        h ^= h >> 16;
        return h;
    }
    
    /// <summary>
    /// Picks a pseudorandom outcome from a list of weighted (probabilistic) choices according to the given weights.
    /// i.e. (Red = 3, Blue = 2 and Green = 1) would create a line of length 6, with a 50% red segment, 33% blue and 17% green. We then sample a pseudorandom point on that line, therefore giving
    /// a proportional probability of selecting any of the given outcomes.
    /// </summary>
    /// <param name="items">A read-only list of possible items to choose from, where every item is of type T.</param>
    /// <param name="weightOf">A delegate function (T => int) where the function describes what the weight (int) is of each item T.</param>
    /// <param name="a">The dependent variable (i.e. what is used to reproduce the same output). e.g. if you want determinism by position, pass in the x position here.</param>
    /// <param name="salt">Unique cryptographic salt.</param>
    /// <typeparam name="T">The type of items we're picking over.</typeparam>
    /// <returns>The picked item, or throws if none could be found.</returns>
    /// <exception cref="InvalidOperationException">Something wrong with the weights (gave negative or all zero weights).</exception>
    public static T RoulettePicker<T>(IReadOnlyList<T> items, Func<T, int> weightOf, int a, int salt)
    {
        int total = 0;
        foreach (T item in items) total += weightOf(item);
        if (total <= 0) throw new InvalidOperationException("Weighted pick with zero total weight.");

        int r = HashRange(0, total, a, salt);
        foreach (T item in items)
        {
            r -= weightOf(item);
            if (r < 0) return item;
        }

        throw new InvalidOperationException("Error with the roulette picker.");
    }

    /// <summary>
    /// For when something must depend on multiple variables for stable deterministic randomness rather than just one variable + a salt, use the combine function first then pass that as the a parameter.
    /// </summary>
    /// <param name="a">Variable 1 to combine. (remember that these variables must be what the "random" choice is dependent on)</param>
    /// <param name="b">Variable 2 to combine.</param>
    /// <returns>A hashed, combined integer ready to go through the pseudorandom system.</returns>
    public static int Combine(int a, int b) => (int)HashUint(a, b);

}
