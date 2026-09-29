using UnityEngine;

/// <summary>
/// A saveable and loadable object that must persist throughout play sessions.
/// </summary>
/// <typeparam name="TModel">The class being used to store data relating to this entity.</typeparam>

public interface IEntity<TModel> where TModel : ISaveRecord
{
    /// <summary>
    /// Loads this entity's data model and populates the MonoBehaviour's corresponding fields/invokes corresponding methods to do with this object's life cycle in this play session.
    /// </summary>
    /// <param name="model"></param>
    public void Bind(TModel model);
}
