using HarmonyLib;
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
}
