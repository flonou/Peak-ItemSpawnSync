using HarmonyLib;
using ItemSpawnSync.Core;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FFSPeak.Patches
{
    /// <summary>
    /// Harmony patch to intercept Spawner.SpawnItems calls for synchronization
    /// Patches all SpawnItems methods (base and overridden) in one go
    /// </summary>
    [HarmonyPatch]
    public class SpawnerSpawnItemsPatch
    {
        /// <summary>
        /// Dynamically find all SpawnItems methods to patch (base Spawner and all derived classes)
        /// </summary>
        static IEnumerable<MethodBase> TargetMethods()
        {
            // Get all types that inherit from Spawner
            var spawnerTypes = new List<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(Spawner).IsAssignableFrom(t));
                    spawnerTypes.AddRange(types);
                }
                catch
                {
                    // Skip assemblies that can't be reflected
                }
            }

            // Find SpawnItems method in each type (including inherited ones)
            foreach (var type in spawnerTypes)
            {
                var method = type.GetMethod("SpawnItems", 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null,
                    new[] { typeof(List<Transform>) },
                    null);

                if (method != null)
                {
                    yield return method;
                }
            }
        }

        static bool Prefix(Spawner __instance, List<Transform> spawnSpots, ref List<PhotonView> __result)
        {
            var manager = SpawnerSyncManager.Instance;
            
            // If manager doesn't exist, let original method run
            if (manager == null)
                return true;

            // If activated, check if we have data for this spawner
            if (manager.UseLoadedSpawnData)
            {
                if (manager.HasSpawnerData(__instance))
                {
                    // Get the saved data and spawn from it
                    var spawnerData = manager.GetSpawnerData(__instance);
                    __result = manager.SpawnItemsFromData(__instance, spawnerData);
                    // Skip original method - we're replaying from saved data
                    return false;
                }
                else
                {
                    // No data for this spawner - don't spawn anything
                    __result = new List<PhotonView>();
                    return manager.SpawnIfNoDataFound;
                }
            }

            __result = new List<PhotonView>();
            return !manager.DisableSpawning || manager.CapturingSpawn;
        }
    }
}
