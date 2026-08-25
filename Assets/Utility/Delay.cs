using UnityEngine;
using System.Collections;

/**
 * Provides real-time related functionality such as waiting for n seconds. 
*/

public static class Delay
{
    /// <summary>
    /// Waits for a given number of seconds before triggering some callback. *NOTE* THIS WILL EXECUTE UNLESS CANCELLED. MAKE SURE TO CANCEL CALLBACKS IF THEY NEED TO BE IGNORED IN SOME CASES.
    /// </summary>
    /// <param name="host">The original caller of this delay.</param>
    /// <param name="seconds">How many seconds to wait, not necessarily a whole number.</param>
    /// <param name="action">The callback after the delay elapses.</param>
    /// <returns></returns>
    public static Coroutine WaitThen(MonoBehaviour host, float seconds, System.Action action)
        => host.StartCoroutine(Wait(seconds, action));

    public static Coroutine Wait(MonoBehaviour host, float seconds)
        => host.StartCoroutine(Wait(seconds, null));

    /// <summary>
    /// Cancels a given delay timer so that the callback is not executed.
    /// </summary>
    /// <param name="host">The MonoBehaviour instance that requested the delay.</param>
    /// <param name="handle">The Coroutine instance of the delay.</param>
    public static void Cancel(MonoBehaviour host, Coroutine handle)
    {
        if (handle != null) host.StopCoroutine(handle);
    }

    private static IEnumerator Wait(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke(); // null actions are never invoked and instead ignored thanks to ?.()
    }
}