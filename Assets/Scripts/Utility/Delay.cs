using UnityEngine;
using System.Collections;

/**
 * Provides real-time related functionality such as waiting for n seconds. 
*/

public static class Delay
{
    /// <summary>
    /// Waits for a given number of seconds before triggering some callback.
    /// </summary>
    /// <param name="host">The original caller of this delay.</param>
    /// <param name="seconds">How many seconds to wait, not necessarily a whole number.</param>
    /// <param name="action">The callback after the delay elapses.</param>
    /// <returns></returns>
    public static Coroutine WaitThen(MonoBehaviour host, float seconds, System.Action action)
        => host.StartCoroutine(Wait(seconds, action));

    private static IEnumerator Wait(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke(); // null actions are never invoked and instead ignored thanks to ?.()
    }
}