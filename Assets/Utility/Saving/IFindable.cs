/// <summary>
/// The contract for any "findable entity" that needs to be persistent in the game.
/// Examples of this includes buildings, NPCs or world assets that need to be saved and loaded and might be modified.
/// </summary>

public interface IFindable
{
    /// <summary>
    /// Stable property used to save references between entities, such as a lumberjack storing a reference to a specific saved tree.
    /// </summary>
    public int Id { get; }
}
