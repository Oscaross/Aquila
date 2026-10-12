using UnityEngine;

/// <summary>
/// Any component responsible for building state, such as instantiating objects that must persist or doing spawning logic must extend this abstract class.
/// </summary>

public abstract class GameManagerComponent : MonoBehaviour
{
    /// <summary>
    /// Called by the save loader system. Each manager must handle reading its relevant state data parameters and reconstructing them.
    /// </summary>
    /// <param name="state">The deserialised save file containing all of our data models and what they held last save.</param>
    /// <param name="context">The save-specific context, for example, a reference to the registry that maps IDs to entities.</param>
    public abstract void Restore(WorldState state, WorldContext context);
    /// <summary>
    /// If this manager is responsible for instantiating parts of the game in a new save, override this method and write logic that is to only run when a new save is first created.
    /// NOTE: Do NOT write code that reconstructs from existing state in here, that belongs in the Restore(WorldState s) method.
    /// For example, the zone manager overrides this to generate the legion, forest, arable zones that the player first gets when they start a new run.
    /// </summary>
    public virtual void GenerateNewWorld() {}
    /// <summary>
    /// Each manager must provide the save-wide EntityRegistry object with a reference to each IFindable object. That is, any object that this manager is responsible for with an ID (i.e. it might be cross-referenced
    /// by some other component in the game) MUST be passed to the registry object before the game can proceed. 
    /// </summary>
    /// <param name="context">The save-wide context containing things like the registry, which contains the ID => Instance mappings for any IFindable entity.</param>
    public virtual void ResolveReferences(WorldContext context) {}

    /// <summary>
    /// Spawns an instance of the object that the data model stored in JSON defines, registering it with the registry, calling its Bind method and putting it into the world.
    /// </summary>
    /// <param name="prefab">The prefab instance of the object to spawn.</param>
    /// <param name="record">The data container specifying the data parameters of the specific object.</param>
    /// <param name="position">Where it should be spawned (usually contained in the data model itself).</param>
    /// <param name="context">The object containing context about the current save like the registry.</param>
    /// <typeparam name="TView">The view of the data model. This is the version that the user sees and is re-instantiated each time a save is loaded. It is completely derived from the data model
    /// and must be a MonoBehaviour as well as an Entity and IFindable, meaning it a stable ID that can be looked up and referred to by other objects in code.</typeparam>
    /// <typeparam name="TModel">The data model that has been stored and is being reconstructed into the game. It must be an ISaveRecord so that the save system knows to serialise it into JSON.</typeparam>
    /// <returns></returns>
    protected TView Spawn<TView, TModel>(TView prefab, TModel record, Vector3 position, WorldContext context)
        where TView : MonoBehaviour, IEntity<TModel>, IFindable
        where TModel : ISaveRecord
    {
        TView view = Instantiate(prefab, position, Quaternion.identity, transform);
        view.Bind(record, context);
        context.Registry.Register(view);
        return view;
    }
}
