using HarmonyLib;

using ItemSpawnSync.Core;

using Peak;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace ItemSpawnSync.Patches
{
    /// <summary>
    /// Harmony patch to fix null reference in SpawnedItemTracker.SpawnAndTrackFromItemHistory
    /// </summary>
    [HarmonyPatch(typeof(SpawnedItemTracker), "SpawnAndTrackFromItemHistory")]
    public class SpawnedItemTrackerPatch
    {
        static bool Prefix(SpawnedItemTracker __instance, ref List<PhotonView> __result, ref List<SpawnedItemTracker.SpawnRecord> ____historyFromSave)
        {
            // Check if _historyFromSave is null
            if (____historyFromSave == null)
            {
                ItemSpawnSync.Plugin.Log?.LogWarning($"{__instance.name}: _historyFromSave is null, initializing empty list");
                
                // Initialize with empty list
                ____historyFromSave = new List<SpawnedItemTracker.SpawnRecord>();
                
                // Return empty list result and skip original method
                __result = new List<PhotonView>();
                return true;
            }

            // If not null, let the original method run
            return true;
        }
    }

    [HarmonyPatch(typeof(SpawnedItemTracker), nameof(SpawnedItemTracker.HasSpawnHistory), MethodType.Getter)]
    public class SpawnedItemTrackerHasSpawnHistoryPatch
    {
        static bool Prefix(SpawnedItemTracker __instance, ref bool __result)
        {
        
            var manager = SpawnerSyncManager.Instance;
            
            // If manager doesn't exist, let original method run
            if (manager == null)
                return true;

            // When replaying loaded spawn data, force false and skip original getter.
            if (manager.UseLoadedSpawnData)
            {
                __result = false;
                return false;
            }

            // Otherwise, run original getter.
            return true;
        }
    }
}
