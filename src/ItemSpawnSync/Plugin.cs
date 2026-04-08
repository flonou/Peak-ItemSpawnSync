using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;

using ItemSpawnSync.Core;
using ItemSpawnSync.Data;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItemSpawnSync;

// Here are some basic resources on code style and naming conventions to help
// you in your first CSharp plugin!
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces

// This BepInAutoPlugin attribute comes from the Hamunii.BepInEx.AutoPlugin
// NuGet package, and it will generate the BepInPlugin attribute for you!
// For more info, see https://github.com/Hamunii/BepInEx.AutoPlugin
[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    #region properties
    public static Plugin? Instance { get; private set; }
    public bool IsSpawnDataLocked => SpawnerSyncManager.Instance.IsDataLocked;
    public KeyboardShortcut LoadDataKey => LoadDataKeyConfig!.Value;
    public static ManualLogSource? Log { get; private set; }
    public KeyboardShortcut SaveDataKey => SaveDataKeyConfig!.Value;
    public KeyboardShortcut TriggerSpawnKey => TriggerSpawnKeyConfig!.Value;
    public KeyboardShortcut ExportLootDataKey => ExportLootDataKeyConfig!.Value;
    public KeyboardShortcut ImportLootDataKey => ImportLootDataKeyConfig!.Value;
    public static string DataDirectory => dataDirectory!;

    #endregion 

    #region fields

    // Configuration
    public bool EnableKeyTrigger = true;

    public ConfigEntry<bool>? DisableSpawningConfig;
    public ConfigEntry<string>? FileNameConfig;
    public ConfigEntry<KeyboardShortcut>? LoadDataKeyConfig;
    public ConfigEntry<KeyboardShortcut>? ExportLootDataKeyConfig;
    public ConfigEntry<KeyboardShortcut>? SaveDataKeyConfig;
    public ConfigEntry<bool>? SpawnIfNoDataFoundConfig;
    public ConfigEntry<KeyboardShortcut>? TriggerSpawnKeyConfig;
    public ConfigEntry<KeyboardShortcut>? ImportLootDataKeyConfig;
    private static string? dataDirectory;
    private Harmony? harmony;

    private static readonly JsonSerializerSettings jsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Formatting = Formatting.None,
        ContractResolver = new ContractResolver()
    };

    #endregion 

    #region methods

    /// <summary>
    /// Get spawn data as JSON string (for network transmission)
    /// </summary>
    public string GetSpawnDataAsJson()
    {
        MapSpawnerData? data = SpawnerSyncManager.Instance.CurrentMapData;
        return JsonConvert.SerializeObject(data);
    }

    /// <summary>
    /// Load spawn data from external source (network, mod communication, etc.)
    /// </summary>
    /// <param name="data">Spawn data to load</param>
    /// <param name="lockData">If true, prevents further data loading permanently</param>
    /// <returns>True if data was loaded, false if blocked by lock</returns>
    public bool LoadSpawnDataInCurrentLevel(MapSpawnerData data, bool lockData = false)
    {
        bool success = SpawnerSyncManager.Instance.LoadMapSpawnerDataInCurrentLevel(data, lockData);
        if (success)
        {
            Log!.LogInfo($"Loaded spawn data with {data.Spawners.Count} spawners" + (lockData ? " (LOCKED)" : ""));
        }
        
        // If spawning is disabled, spawn items from loaded data
        if (SpawnerSyncManager.Instance.DisableSpawning)
                SpawnerSyncManager.Instance.SpawnItemsFromStartSpawners();
        return success;
    }

    /// <summary>
    /// Load spawn data from file
    /// </summary>
    public MapSpawnerData? LoadSpawnDataFromFile(string filename)
    {
        try
        {
            string filepath = Path.Combine(dataDirectory, filename);

            if (!File.Exists(filepath))
            {
                Log!.LogError($"Spawn data file not found: {filepath}");
                return null;
            }

            string json = File.ReadAllText(filepath);
            MapSpawnerData? data = JsonConvert.DeserializeObject<MapSpawnerData>(json);

            if (data != null)
            {
                Log!.LogInfo($"Loaded spawn data from: {filepath}");
                Log!.LogInfo($"  Spawners: {data.Spawners.Count}");
                Log!.LogInfo($"  Items: {CountTotalItems(data)}");
            }
            else
                Log!.LogError($"Failed to deserialize spawn data from json {json}");
            return data;
        }
        catch (Exception ex)
        {
            Log!.LogError($"Failed to load spawn data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load spawn data from JSON string
    /// </summary>
    /// <param name="json">JSON string containing spawn data</param>
    /// <param name="lockData">If true, prevents further data loading permanently</param>
    /// <returns>True if data was loaded, false if blocked by lock</returns>
    public bool LoadSpawnDataFromJson(string json, bool lockData = false)
    {
        try
        {
            MapSpawnerData? data = JsonConvert.DeserializeObject<MapSpawnerData>(json);
            if (data != null)
                return true;
            Log!.LogError($"Could not deserialize spawn data from JSON: {json}");
            return false;
        }
        catch (Exception ex)
        {
            Log!.LogError($"Failed to load spawn data from JSON: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save current spawn data to file
    /// </summary>
    public void SaveCurrentSpawnData()
    {
        try
        {
            MapSpawnerData data = SpawnerSyncManager.Instance.CurrentMapData;
            if (data == null || data.Spawners.Count == 0)
            {
                Log!.LogWarning("No spawn data to save. Start capture first and trigger spawners.");
                return;
            }
            // get current map name
            string currentMap = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string filename = $"{currentMap}_spawn_data_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filepath = Path.Combine(dataDirectory, filename);

            string json = JsonConvert.SerializeObject(data, Formatting.Indented, jsonSettings);
            File.WriteAllText(filepath, json);

            Log!.LogInfo($"Saved spawn data to: {filepath}");
            Log!.LogInfo($"  Spawners: {data.Spawners.Count}");
            Log!.LogInfo($"  Items: {CountTotalItems(data)}");
        }
        catch (Exception ex)
        {
            Log!.LogError($"Failed to save spawn data: {ex.Message}");
        }
    }

    protected void Awake()
    {
        Instance = this;
        Log = base.Logger;

        TriggerSpawnKeyConfig = Config.Bind("Controls", "TriggerSpawnKey", new KeyboardShortcut(KeyCode.F4, KeyCode.LeftControl), "Key to trigger all spawners");
        SaveDataKeyConfig = Config.Bind("Controls", "SaveDataKey", new KeyboardShortcut(KeyCode.F5, KeyCode.LeftControl), "Key to save current spawn data to file");
        LoadDataKeyConfig = Config.Bind("Controls", "LoadDataKey", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl), "Key to load spawn data from file");
        ExportLootDataKeyConfig = Config.Bind("Controls", "ExportLootDataKey", new KeyboardShortcut(KeyCode.F7, KeyCode.LeftControl), "Key to export loot data to file");
        ImportLootDataKeyConfig = Config.Bind("Controls", "ImportLootDataKey", new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl), "Key to import loot data from file");
        DisableSpawningConfig = Config.Bind("General", "DisableSpawning", false, "If true, prevents any item spawning unless from loaded data");
        SpawnIfNoDataFoundConfig = Config.Bind("General", "SpawnIfNoDataFound", false, "If true, spawners without loaded data will spawn normally");
        FileNameConfig = Config.Bind("General", "DefaultFileName", "spawn_data.json", "Default filename to load spawn data.");

        dataDirectory = Path.Combine(Paths.ConfigPath, "ItemSpawnSync");
        Directory.CreateDirectory(dataDirectory);

        // Initialize manager
        SpawnerSyncManager.Instance.Initialize(Log);
        SpawnerSyncManager.Instance.EnableKeyTrigger = EnableKeyTrigger;
        SpawnerSyncManager.Instance.DisableSpawning = DisableSpawningConfig.Value;
        SpawnerSyncManager.Instance.SpawnIfNoDataFound = SpawnIfNoDataFoundConfig.Value;

        Log.LogInfo($"Plugin {Name} is loaded!");
        Log.LogInfo($"Data directory: {dataDirectory}");

        // Apply Harmony patches
        harmony = new Harmony(Name);
        harmony.PatchAll();

        Log.LogInfo("Harmony patches applied");
        Log.LogInfo($"Controls:");
        Log.LogInfo($"  {TriggerSpawnKey} - Trigger all spawners");
        Log.LogInfo($"  {SaveDataKey} - Save spawn data to file");
        Log.LogInfo($"  {LoadDataKey} - Load spawn data from file");
        Log.LogInfo($"  {ExportLootDataKey} - Export loot data to file");
        Log.LogInfo($"  {ImportLootDataKey} - Import loot data from file");

        
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    protected void OnSceneUnloaded(Scene arg0)
    {
        if (arg0.name.StartsWith("Level_"))
        {
            // When a level is unloaded, we should clear the current spawn data to avoid conflicts with new scene
            SpawnerSyncManager.Instance.ClearSpawnedData(arg0.name);
        }
    }

    protected void Update()
    {
        // Trigger all spawners
        if (TriggerSpawnKey.IsDown())
        {
            SpawnerSyncManager.Instance.CaptureSpawns();
        }
        // Save spawn data
        if (SaveDataKey.IsDown())
        {
            SaveCurrentSpawnData();
        }

        // Load spawn data
        if (LoadDataKey.IsDown())
        {
            MapSpawnerData? spawnerData = LoadSpawnDataFromFile(FileNameConfig!.Value);
            if (spawnerData != null)
                LoadSpawnDataInCurrentLevel(spawnerData);
        }

        // Export loot data
        if (ExportLootDataKey.IsDown())
        {
            LootDataContainer.ExportLootData();
        }
        // Import loot data
        if (ImportLootDataKey.IsDown())
        {
            LootDataContainer.ImportLootData();
        }
    }



    private int CountTotalItems(MapSpawnerData data)
    {
        int count = 0;
        foreach (SpawnerInstanceData spawner in data.Spawners)
        {
            count += spawner.SpawnedItems.Count;
        }
        return count;
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }

    #endregion 
}