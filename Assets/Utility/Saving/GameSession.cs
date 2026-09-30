using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSession : MonoBehaviour
{
    [Header("Save System Bypass")]
    [SerializeField] private StartMode startMode = StartMode.NewGame;
    [SerializeField] private int devSeed = 12345;
    [Tooltip("Example save file that we want to load.")]
    [SerializeField] private TextAsset devScenario;
    
    private string DevSavePath => Path.Combine(Application.persistentDataPath, "dev.json");
    
    [SerializeField] private GameManagerComponent[] gameManagers;

    /// <summary>
    /// The current save file that we are using 
    /// </summary>
    public WorldState State { get; private set; }
    public WorldContext Context { get; private set; }
    
    public bool IsWorldReady { get; private set; }
    /// <summary>
    /// Event that fires once world generation and placement is finished. Subscribe methods to this event that observe the world and can't run until immediately after the world is generated.
    /// </summary>
    public event Action WorldReady;
    
    // Setting config
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = new SaveTypeBinder() // this tells Newtonsoft how to map our class names into JSON attributes i.e. "ForestZoneData: {data}"
    };

    public void LoadSave(string path) => LoadFromJson(File.ReadAllText(path));
    
    private void LoadFromJson(string json)
    {
        try
        {
            Begin(JsonConvert.DeserializeObject<WorldState>(json, Settings));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while parsing save file! Is it corrupt? Error message: {e.Message}.");
            throw;
        }
        
        RaiseWorldReady();
        Debug.Log("Successfully loaded save!");
    }

    private void RaiseWorldReady()
    {
        IsWorldReady = true;
        WorldReady?.Invoke();
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
        
        Debug.Log("Save success! Output path: " + path);
    }

    public void NewGame(int seed)
    {
        Debug.Log("Generating a fresh save (a new game, not loading from a save file).");
        Begin(new WorldState { Seed = seed });
        foreach (var manager in gameManagers) manager.GenerateNewWorld();
        
    }
    
    private void Begin(WorldState state)
    {
        State = state;
        Context = new WorldContext(new EntityRegistry());
        
        foreach (var manager in gameManagers) manager.Restore(State, Context);
        foreach (var manager in gameManagers) manager.ResolveReferences(Context);
    }

    private void Start()
    {
        LaunchRequest request = LaunchRequest.Consume();

        if (request.SavePath != null) LoadSave(request.SavePath);
        else if (request.Seed.HasValue) NewGame(request.Seed.Value);
        else                            StartFromInspector();
    }
    
    private void StartFromInspector()
    {
        if (startMode == StartMode.Scenario && devScenario != null) LoadFromJson(devScenario.text);
        else NewGame(devSeed);
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame) Save(DevSavePath);
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            LaunchRequest.Set(new LaunchRequest { SavePath = DevSavePath });
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
#endif
}

public enum StartMode { NewGame, Scenario }