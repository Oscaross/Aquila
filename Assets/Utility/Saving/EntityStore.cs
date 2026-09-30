using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class EntityStore<T> where T : EntityData
{
    [JsonProperty] private readonly List<T> items = new();
    [JsonIgnore] public IReadOnlyList<T> Items => items;

    public T Add(T record, IdAllocator ids)
    {
        // Check that we've not got an ID for this entity already
        if (record.Id != 0) throw new InvalidOperationException($"{typeof(T).Name} already has id {record.Id}; added twice?");

        record.Id = ids.AllocateNewID();
        Debug.Log($"Successfully saved entity record for {record.GetType()}");
        items.Add(record);
        return record;
    }
}
