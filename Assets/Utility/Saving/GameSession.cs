using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the startup phase of loading an existing/new save. Sends command signals, callbacks and fires manager scripts to trigger the building of a new game instance.
/// Single authority on game sessions, so it is imperative it runs before everything else (hence the -1000 exec order).
/// </summary>

[DefaultExecutionOrder(-1000)]
public class GameSession : MonoBehaviour
{
    [Header("Save System Bypass")] [SerializeField]
    private StartMode startMode = StartMode.NewGame;

    [SerializeField] private int devSeed = 12345;

    [Tooltip("Example save file that we want to load.")] [SerializeField]
    private TextAsset devScenario;

    [SerializeField] private bool logSavingToConsole;

    /// <summary>
    /// The active instance of the current game session, allowing any script in the codebase to give methods to be run only after the world is configured.
    /// </summary>
    public static GameSession Current { get; private set; }

    private event Action worldReady;

    public static bool LogSavingToConsole;

    private void OnValidate() => LogSavingToConsole = logSavingToConsole;

    private void Awake()
    {
        if (Current != null && Current != this) Debug.LogError("More than one GameSession allocated to this scene!");
        Current = this;
        LogSavingToConsole = logSavingToConsole;

        Time.fixedDeltaTime =
            1f / Ticks.PerSecond; // we MUST override the project setting for FixedUpdate()'s frequency to use our custom one
    }

    private string DevSavePath => Path.Combine(Application.persistentDataPath, "dev.json");

    [Tooltip(
        "Game managers are responsible for configuring and rebuilding the world around them from given state. Each sector of the game (i.e. subjects, buildings, ...) should have its own manager.")]
    [SerializeField]
    private GameManagerComponent[] gameManagers;

    /// <summary>
    /// The current save file that we are using 
    /// </summary>
    public WorldState State { get; private set; }

    public WorldContext Context { get; private set; }
    /// <summary>
    /// How many ticks (calls of FixedUpdate(), set in Ticks.cs but I made it 50 when I wrote this comment) have occurred since this save was created. Use to derive "time elapsed" anywhere.
    /// </summary>
    public long TicksElapsed { get; private set; }

    private void FixedUpdate()
    {
        TicksElapsed++;
    }

    [Tooltip("Object that all parts of the game use that involve balance. Includes information like yield from trees, how long trees take to grow, yield multipliers in winter, etc...")]
    [SerializeField] private GameBalance gameBalance;

    private bool isWorldReady;

    public static void WhenWorldReady(Action callback)
    {
        if (Current == null) { Debug.LogError("No GameSession instance exists in the scene."); }

        if (Current.isWorldReady) callback();
        else Current.worldReady += callback;
    }
    
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
        catch (JsonException e)
        {
            Debug.LogError("Save file could not be parsed. Is it corrupt?");
            Debug.LogException(e);
            return;
        }
        
        // any non-JSON error escapes here then gets thrown anyways, which is good because it means it's a bug not caused by the core save architecture itself but rather another part
        
        RaiseWorldReady();
        Debug.Log("Successfully loaded save!");
    }

    private void RaiseWorldReady()
    {
        isWorldReady = true;
        Action handlers = worldReady;
        worldReady = null; // auto-unsubscribe everyone that was waiting for us to be ready
        handlers?.Invoke(); // fire every subscriber's ready event 
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
        worldReady = null;
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
        Context = new WorldContext(new EntityRegistry(), gameBalance);
        
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
    
    // temporary, allows us to just use f5 and f9 to test whether the game can rebuild itself and save everything it needs to competently. Later this can just be refactored into a UI endpoint
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