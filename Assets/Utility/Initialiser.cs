using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put any code that must run globally for the game to function, irrespective of legion or state before the game begins. This script will run at the start of the game, as long as the main camera object is active.
/// </summary>

[DefaultExecutionOrder(-100)]
public class Initialiser : MonoBehaviour
{
    private void Awake()
    {
        ConfigureInputSystemMaps();
    }

    private void ConfigureInputSystemMaps()
    {
        InputSystem.actions.FindActionMap("Gameplay", true).Enable(); // the gameplay map starts on always
        
        // All other maps start as disabled
        InputSystem.actions.FindActionMap("Building", true).Disable();
    }
}
