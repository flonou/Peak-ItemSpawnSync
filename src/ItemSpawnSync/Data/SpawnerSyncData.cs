using System;
using System.Collections.Generic;

using UnityEngine;

namespace ItemSpawnSync.Data
{
    /// <summary>
    /// Data for a single spawned item at a specific location
    /// </summary>
    [Serializable]
    public class SpawnedItemData
    {
        public string ItemPrefabName = string.Empty;        // Name of the spawned item prefab
        public Vector3 Position;             // Position of the spawn spot
        public Quaternion Rotation;          // Rotation of the spawn spot
        public int ViewID;                // View ID of original spawned item (if applicable)
    }

    /// <summary>
    /// Data for all items spawned by a single spawner instance
    /// </summary>
    [Serializable]
    public class SpawnerInstanceData
    {
        public string SpawnerTypeName = string.Empty;       // Type name (Luggage, BerryBush, etc.)
        public int SpawnerInstanceID;        // Unique ID for this spawner instance
        public Vector3 SpawnerPosition;      // Position of the spawner
        public List<SpawnedItemData> SpawnedItems = [];
    }

    /// <summary>
    /// Complete spawning data for the entire map/session
    /// </summary>
    [Serializable]
    public class MapSpawnerData
    {
        public List<SpawnerInstanceData> Spawners = [];
    }
}
