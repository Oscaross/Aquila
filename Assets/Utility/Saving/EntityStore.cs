using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// A generalised container for any save object that needs to store a list of referrable entities in save data.
/// </summary>
/// <typeparam name="T">Any entity that declares an ID and is therefore findable in the game, such as an NPC or fellable tree.</typeparam>

public class EntityStore<T> where T : EntityData
{
    [JsonProperty] private readonly List<T> items = new();
    [JsonIgnore] public IReadOnlyList<T> Items => items;

    /// <summary>
    /// Adds an object to the save records of this collection and automatically assigns it a valid ID if it has not already been added.
    /// Items which are already saved will throw because they must not overwrite existing save IDs. Use the Remove() function to remove an object from save memory if that is what you wish to do.
    /// </summary>
    /// <param name="record">The data record, of type EntityData (must declare an Id interface).</param>
    /// <returns>The record of the object added, throws if the object wasn't added.</returns>
    /// <exception cref="InvalidOperationException">If saving this object would be invalid, e.g. the record was already assigned an ID and thus already saved.</exception>
    public T Add(T record)
    {
        // A non-zero ID implies that this record has already been assigned an ID
        if (record.Id != 0) throw new InvalidOperationException($"{typeof(T).Name} already has id {record.Id}; added twice?");
        
        record.Id = GameSession.Current.State.IdAllocator.AllocateNewID();
        items.Add(record);
        
        if (GameSession.LogSavingToConsole) Debug.Log($"Added {typeof(T)} record with id {record.Id}");
        
        return record;
    }
}
