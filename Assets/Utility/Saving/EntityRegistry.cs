using System.Collections.Generic;
using UnityEngine;

public class EntityRegistry
{
    private readonly Dictionary<int, IFindable> entities = new();

    public void Register(IFindable entity)
    {
        Debug.Assert(entity.Id != 0, $"{entity} has been registered without a valid ID!");
        
        entities.Add(entity.Id, entity);
    }

    public void Unregister(IFindable entity) => entities.Remove(entity.Id);

    /// <summary>
    /// Attempts to fetch the entity that matches the given ID. Returns true and the entity as an out parameter if it was found, else returns false.
    /// </summary>
    /// <param name="id">The ID of the entity to be returned.</param>
    /// <param name="entity">The instance of the entity.</param>
    /// <typeparam name="T">The desired type of the entity instance, where it is IFindable (recall that only IFindable entities have IDs).</typeparam>
    /// <returns>True if entity is found, false otherwise (and entity is null).</returns>
    public bool TryGet<T>(int id, out T entity) where T : class, IFindable
    {
        entity = entities.TryGetValue(id, out IFindable e) ? e as T : null;
        return entity != null;
    }
}
