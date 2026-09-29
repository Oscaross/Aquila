using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private GameManagerComponent[] gameManagers;

    /// <summary>
    /// The current save file that we are using 
    /// </summary>
    public WorldState State { get; private set; }
    
    // Setting config
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = new SaveTypeBinder() // this tells Newtonsoft how to map our class names into JSON attributes i.e. "ForestZoneData: {data}"
    };

    public void LoadSave(string path)
    {
        string json = File.ReadAllText(path);
        State = JsonConvert.DeserializeObject<WorldState>(json, Settings);
            
        foreach (var manager in gameManagers)
        {
            manager.Restore(State);
        }
    }

    public void Save(string path)
    {
        State.SavedAtUtc = DateTime.UtcNow;
        string json = JsonConvert.SerializeObject(State, Settings);

        string dir = Path.GetDirectoryName(path);
        
        if (dir == null)
        {
            Debug.LogError("Save directory path not found! Saving has failed.", this);
            return;
        }

        Directory.CreateDirectory(dir);
        string temp = path + ".tmp";
        File.WriteAllText(temp, json);
        
        // Swap new save in, keep old one as backup or just move new save in if an old one doesn't exist.
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
        else File.Move(temp, path);
    }
}
