using UnityEngine;

public struct LaunchRequest
{
    public string SavePath; // either load this save path
    public int? Seed; // or a new world with this seed?

    private static LaunchRequest pending;

    public static void Set(LaunchRequest request) => pending = request;

    public static LaunchRequest Consume()
    {
        LaunchRequest r = pending;
        pending = default;
        return r;
    }

    // Clear leftovers when entering play mode so we can test using the save/load system
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => pending = default;
}
