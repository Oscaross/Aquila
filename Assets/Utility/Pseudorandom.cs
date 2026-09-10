using UnityEngine;

/// <summary>
/// When you want deterministic random behaviour. Deterministic random behaviour ("pseudorandomness") is where outcomes/generation appears random but it is directly tied to a seed.
/// The seed is stored on Pseudorandom, anything that generates random values from Pseudorandom will always generate the same "random" outcome if the seed stays the same and the input arguments stay the same.
/// Mainly used in things that need to be reproduced/regenerated during saving.
///
/// Salts are also stored in this class. A salt is just a way to make the random generator give different, uncorrelated answers for different classes. Define a new static readonly salt when
/// we need two classes to ensure they pick random values differently, or reuse one if doing multiple random calls within the same system/system subset.
/// </summary>

public class Pseudorandom : MonoBehaviour
{
    private static int _seed = 910;

    public const int TilePainterIslandSalt = 0x1B873593;
    public const int TilePainterGapSalt = unchecked((int)0x9E3779B9); // this hack gets around overflowing the int limit, we only care about bit position and not value, so this is fine

    /// <summary>
    /// Returns a pseudorandom value between 0 and 1, analogous to the Random.value function.
    /// </summary>
    /// <param name="a">The first value to determine the outcome, such as the position of an object.</param>
    /// <param name="b">The second value to determine the outcome, usually we want a salt here that stays the same for all random outcomes of this nature.</param>
    /// <returns>A pseudorandom floating point number between 0 and 1.</returns>
    public static float Hash01(int a, int b) => HashUint(a, b, _seed) / 4294967296f;

    /// <summary>
    /// Returns a pseudorandom integer between min (inclusive) and max (exclusive).
    /// </summary>
    /// <param name="min">The minimum (inclusive) integer this could be.</param>
    /// <param name="max">The maximum (exclusive) integer this could be.</param>
    /// <param name="a">The first value to determine the outcome, such as the position of an object.</param>
    /// <param name="b">The second value to determine the outcome, usually we want a salt here that stays the same for all random outcomes of this nature.</param>
    /// <returns>A psuedorandom integer between min (inclusive) and max (exclusive).</returns>
    public static int HashRange(int min, int max, int a, int b)
        => min + (int)(HashUint(a, b, _seed) % (uint)(max - min));
    
    private static uint HashUint(int a, int b, int seed)
    {
        uint h = (uint)(a * 374761393 + b * 668265263 + seed * 2654435761u);
        h = (h ^ (h >> 13)) * 1274126177;
        h ^= h >> 16;
        return h;
    }

}
