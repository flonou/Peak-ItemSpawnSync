using BepInEx.Logging;

using ItemSpawnSync.Data;

using Photon.Pun;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace ItemSpawnSync.Core;

/// <summary>
/// Manages synchronized spawning across all clients by capturing and replaying spawn results
/// </summary>
public class SpawnerSyncManager : MonoBehaviour
{

    #region properties
    public bool CapturingSpawn { get; internal set; }
    public MapSpawnerData? CurrentMapData { get; private set; }

    public static SpawnerSyncManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new("SpawnerSyncManager");
                instance = go.AddComponent<SpawnerSyncManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public bool IsDataLocked { get; private set; } = false;
    public bool UseLoadedSpawnData { get; private set; } = false;
    #endregion 

    #region fields
    public bool DisableSpawning;
    public bool EnableKeyTrigger = false;
    public bool SpawnIfNoDataFound;
    protected MethodInfo ForceSyncForFramesMethod = typeof(Item).GetMethod("ForceSyncForFrames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static SpawnerSyncManager? instance;
    private ManualLogSource? logger;
    private Dictionary<int, Spawner> spawnerRegistry = [];
    private Dictionary<Spawner, SpawnerInstanceData> spawnerToDataMap = [];
    #endregion 

    #region methods

    public void CaptureSpawns()
    {
        CurrentMapData = new MapSpawnerData();
        TriggerAllSpawners();
    }

    /// <summary>
    /// Find all spawner instances in the scene
    /// </summary>
    public static List<Spawner> FindAllSpawnersInScene()
    {
        return FindObjectsByType<Spawner>(FindObjectsSortMode.None).ToList();
    }

    /// <summary>
    /// Get spawner data
    /// </summary>
    public SpawnerInstanceData? GetSpawnerData(Spawner spawner)
    {
        return spawnerToDataMap.TryGetValue(spawner, out SpawnerInstanceData? data) ? data : null;
    }

    /// <summary>
    /// Check if spawner has data
    /// </summary>
    public bool HasSpawnerData(Spawner spawner)
    {
        return spawnerToDataMap.ContainsKey(spawner);
    }

    public void Initialize(ManualLogSource logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Start replaying from provided data (clients use this)
    /// </summary>
    /// <param name="data">Spawn data to replay</param>
    /// <param name="lockData">If true, prevents further data loading permanently</param>
    /// <returns>True if data was loaded, false if blocked by lock</returns>
    public bool LoadMapSpawnerData(MapSpawnerData data, bool lockData = false)
    {
        if (IsDataLocked)
        {
            logger?.LogWarning("Spawn data is locked. Cannot load new data.");
            return false;
        }

        CurrentMapData = data;
        spawnerRegistry.Clear();
        spawnerToDataMap.Clear();

        // Find all current spawners in the scene
        List<Spawner> currentSpawners = FindAllSpawnersInScene();
        HashSet<SpawnerInstanceData> matchedData = [];

        logger?.LogInfo($"Found {currentSpawners.Count} spawners in scene, attempting to match with {data.Spawners.Count} data entries");

        // Phase 1: Match by PhotonView ID (if >= 0)
        int matchedByViewID = 0;
        foreach (Spawner spawner in currentSpawners)
        {
            if (spawner.photonView != null && spawner.photonView.ViewID >= 0)
            {
                SpawnerInstanceData matchingData = data.Spawners.FirstOrDefault(d =>
                    d.SpawnerInstanceID == spawner.photonView.ViewID &&
                    !matchedData.Contains(d));

                if (matchingData != null)
                {
                    spawnerToDataMap[spawner] = matchingData;
                    matchedData.Add(matchingData);
                    matchedByViewID++;
                    logger?.LogInfo($"Matched spawner {spawner.name} by ViewID {spawner.photonView.ViewID}");
                }
            }
        }

        // Phase 2: Match remaining spawners by proximity
        int matchedByProximity = 0;
        List<Spawner> unmatchedSpawners = currentSpawners.Where(s => !spawnerToDataMap.ContainsKey(s)).ToList();
        List<SpawnerInstanceData> unmatchedData = data.Spawners.Where(d => !matchedData.Contains(d)).ToList();

        foreach (Spawner? spawner in unmatchedSpawners)
        {
            // Find closest matching spawner data by type and position
            SpawnerInstanceData matchingData = unmatchedData
                .Where(d => d.SpawnerTypeName == spawner.GetType().Name)
                .OrderBy(d => Vector3.Distance(spawner.transform.position, d.SpawnerPosition))
                .FirstOrDefault();

            if (matchingData != null)
            {
                float distance = Vector3.Distance(spawner.transform.position, matchingData.SpawnerPosition);
                // Only match if reasonably close (within 0.01 unit)
                if (distance < 0.01f)
                {
                    spawnerToDataMap[spawner] = matchingData;
                    matchedData.Add(matchingData);
                    unmatchedData.Remove(matchingData);
                    matchedByProximity++;
                    logger?.LogInfo($"Matched spawner {spawner.name} at position {matchingData.SpawnerPosition} by proximity ({distance:F3}m)");
                }
            }
        }

        logger?.LogInfo($"Matched {matchedByViewID} spawners by ViewID, {matchedByProximity} by proximity. Total: {spawnerToDataMap.Count}/{data.Spawners.Count}");

        // Enable replay mode
        UseLoadedSpawnData = true;

        if (lockData)
        {
            IsDataLocked = true;
            logger?.LogInfo($"Started replaying spawner data with {data.Spawners.Count} spawners (LOCKED)");
        }
        else
        {
            logger?.LogInfo($"Started replaying spawner data with {data.Spawners.Count} spawners");
        }

        return true;
    }

    /// <summary>
    /// Spawn items from saved data
    /// </summary>
    public List<PhotonView> SpawnItemsFromData(Spawner spawner, SpawnerInstanceData data)
    {
        List<PhotonView> spawnedViews = [];

        //_logger?.LogInfo($"Spawning {data.SpawnedItems.Count} items for {data.SpawnerTypeName} at {data.SpawnerPosition} from saved data");

        try
        {
            foreach (SpawnedItemData itemData in data.SpawnedItems)
            {
                Item component = PhotonNetwork.InstantiateItemRoom(itemData.ItemPrefabName, itemData.Position, itemData.Rotation).GetComponent<Item>();
                spawnedViews.Add(component.GetComponent<PhotonView>());
                ForceSyncForFramesMethod.Invoke(component, new object[] { 10 });
                if (component != null && spawner.isKinematic)
                {
                    component.GetComponent<PhotonView>().RPC("SetKinematicRPC", RpcTarget.AllBuffered, new object[]
                    {
                            true,
                            component.transform.position,
                            component.transform.rotation
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError($"Error spawning items from data for spawner {spawner.name}: {ex.Message}");
        }

        return spawnedViews;
    }

    /// <summary>
    /// Spawn items from all spawners that have "spawnOnStart" enabled
    /// </summary>
    public void SpawnItemsFromStartSpawners()
    {
        // Get all spawners that have "spawnOnStart" enabled and are enabled
        List<Spawner> spawners = FindAllSpawnersInScene().Where(s => s.spawnOnStart).ToList();
        logger?.LogInfo($"Spawning items from {spawners.Count} start spawners...");
        foreach (Spawner? spawner in spawners)
        {
            try
            {
                spawner.baseSpawnChance = 1.0f; // Ensure 100% spawn chance
                if (spawner.enabled)
                    spawner.ForceSpawn();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error spawning from start spawner {spawner.name} of type {spawner.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Trigger spawning on all spawners in the scene (useful for luggage, etc.)
    /// </summary>
    public void TriggerAllSpawners()
    {
        List<Spawner> spawners = FindAllSpawnersInScene();
        logger?.LogInfo($"Triggering {spawners.Count} spawners...");

        CapturingSpawn = true;

        int triggered = 0;
        foreach (Spawner spawner in spawners)
        {
            try
            {
                if (TriggerSpawner(spawner))
                {
                    triggered++;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error triggering spawner {spawner.name} of type {spawner.GetType().Name}: {ex.Message}");
            }
        }
        CapturingSpawn = false;
        logger?.LogInfo($"Triggered {triggered} spawners out of {spawners.Count} successfully");
    }

    /// <summary>
    /// Helper to save spawner data from GameObjects
    /// </summary>
    private void SaveSpawnerData(Spawner spawner, List<GameObject> spawnedObjects)
    {
        Type type = spawner.GetType();
        SpawnerInstanceData spawnerData = new()
        {
            SpawnerTypeName = type.Name,
            SpawnerPosition = spawner.transform.position,
            SpawnerInstanceID = spawner.photonView != null ? spawner.photonView.ViewID : -1,
            SpawnedItems = spawnedObjects.Select(obj =>
            {
                // Remove (clone) suffix if present
                string itemName = obj.name;
                if (itemName.EndsWith("(Clone)"))
                {
                    itemName = itemName[..^7];
                }
                return new SpawnedItemData
                {

                    ItemPrefabName = itemName,
                    Position = obj.transform.position,
                    Rotation = obj.transform.rotation
                };
            }
            ).ToList()
        };
        CurrentMapData!.Spawners.Add(spawnerData);
        spawnerToDataMap[spawner] = spawnerData;
    }

    /// <summary>
    /// Trigger a specific spawner to spawn its items
    /// </summary>
    private bool TriggerSpawner(Spawner spawner)
    {
        // If spawner has spawnOnStart enabled, use TrySpawnItems that will spawn using randomization chances
        if (spawner.spawnOnStart)
        {
            List<PhotonView> spawnedItems = spawner.TrySpawnItems();
            logger?.LogInfo($"Triggered spawnOnStart on spawner {spawner.name} and got {spawnedItems.Count} items");
            SaveSpawnerData(spawner, spawnedItems.Select(view => view.gameObject).ToList());
            return true;
        }
        // For other spawners, use reflection to find spawn methods

        // Check if spawner has a specific methods (like Luggage)
        Type type = spawner.GetType();

        // Include FlattenHierarchy to search inherited methods (especially for static members)
        // For instance methods, inheritance is included by default, but we add it for clarity
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;


        MethodInfo getSpawnSpotsMethod = type.GetMethod("GetSpawnSpots", bindingFlags);

        if (getSpawnSpotsMethod != null)
        {
            List<Transform>? spawnSpots = getSpawnSpotsMethod.Invoke(spawner, null) as List<Transform>;
            List<PhotonView> spawnedItems = spawner.SpawnItems(spawnSpots);
            logger?.LogInfo($"Triggered {type.Name}.SpawnItems() on spawner {spawner.name} and got {spawnedItems.Count} items");
            SaveSpawnerData(spawner, spawnedItems.Select(view => view.gameObject).ToList());
            return true;
        }
        else
        {
            logger?.LogWarning($"{type.Name} spawner {spawner.name} does not have expected spawn method GetSpawnSpots.");
            return false;
        }

    }

    #endregion 
}