using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class EntityStore<T> where T : EntityData
{
    [JsonProperty] private readonly List<T> items = new();
    [JsonIgnore] public IReadOnlyList<T> Items => items;

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
