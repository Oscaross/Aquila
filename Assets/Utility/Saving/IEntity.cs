/// <summary>
/// A saveable and loadable object that must persist throughout play sessions.
/// </summary>
/// <typeparam name="TModel">The class being used to store data relating to this entity.</typeparam>

public interface IEntity<TModel> where TModel : ISaveRecord
{
    /// <summary>
    /// Binds this entity's data model to its instance member and populates the MonoBehaviour's corresponding fields/invokes corresponding methods to do with this object's life cycle in this play session. Includes the
    /// save file in the event that this entity needs to fetch a reference from a cached ID from the global registry.
    /// </summary>
    /// <param name="model">The data model of the object we are binding from save data.</param>
    /// <param name="save">The context surrounding this instance particular instance of the save, namely so we can access the registry which maps ID => live instances of IFindable entities.</param>
    public void Bind(TModel model, WorldContext save);
}
