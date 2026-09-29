using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// This tells Newtonsoft how to map our class names to JSON attributes.
/// This is important because if we migrate/change to new model class locations, old save files will break as they no longer point to the same data objects, even if the schema remains the same.
/// This is the simplest version of the binder possible for now, just taking the class name i.e. "FellableTreeModel" and writing its name, rather than full assemblies and namespace paths because
/// if we then move our files around this breaks each save file, which would be very annoying.
/// </summary>

public class SaveTypeBinder : ISerializationBinder
{
    private readonly Dictionary<string, Type> types = new();

    public SaveTypeBinder()
    {
        // Register everything in the codebase that implements the ISaveRecord interface and is not itself an interface or an abstract class with its name rather than assembly path.
        foreach (Type t in typeof(ISaveRecord).Assembly.GetTypes())
            if (typeof(ISaveRecord).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                types.Add(t.Name, t); // throws on duplicate names
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        if (!types.ContainsKey(serializedType.Name) || types[serializedType.Name] != serializedType)
            throw new JsonSerializationException($"{serializedType.Name} is not a registered ISaveRecord.");

        assemblyName = null;
        typeName = serializedType.Name;
    }

    public Type BindToType(string assemblyName, string typeName) =>
        types.TryGetValue(typeName, out Type t)
            ? t
            : throw new JsonSerializationException($"Unknown save type '{typeName}'.");
}
