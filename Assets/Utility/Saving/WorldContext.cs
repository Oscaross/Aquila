/// <summary>
/// Object passed around to all managers containing important information about the current save instance, such as the registry which maps IDs to live instances of objects.
/// </summary>

public class WorldContext
{
    public WorldContext(EntityRegistry registry, GameBalance balance)
    {
        Registry = registry;
        Balance = balance;
    }
    
    public GameBalance Balance { get; private set; }
    public EntityRegistry Registry { get; private set; }
}
