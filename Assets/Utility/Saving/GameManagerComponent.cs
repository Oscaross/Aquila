using UnityEngine;

/// <summary>
/// Any component responsible for building state, such as instantiating objects that must persist or doing spawning logic must extend this abstract class.
/// </summary>

public abstract class GameManagerComponent : MonoBehaviour
{
    /// <summary>
    /// Called by the save loader system. Each manager must handle reading its relevant state data parameters and reconstructing them.
    /// </summary>
    /// <param name="state"></param>
    public abstract void Restore(WorldState state);
    public virtual void ResolveReferences() {}
}
