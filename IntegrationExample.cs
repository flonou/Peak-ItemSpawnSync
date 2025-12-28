// Example: Integrating ItemSpawnSync with FFSPeak network plugin

using ItemSpawnSync;
using ItemSpawnSync.Core;
using System.Linq;

namespace Example
{
    public class ItemSpawnSyncIntegration
    {
        private static Plugin _itemSpawnPlugin;
        
        public static void Initialize()
        {
            // Find the ItemSpawnSync plugin
            var pluginInfo = BepInEx.Bootstrap.Chainloader.PluginInfos
                .FirstOrDefault(p => p.Value.Metadata.GUID == "ItemSpawnSync");
            
            if (pluginInfo.Value != null)
            {
                _itemSpawnPlugin = pluginInfo.Value.Instance as Plugin;
                Plugin.Logger.LogInfo("ItemSpawnSync plugin found and integrated");
            }
            else
            {
                Plugin.Logger.LogWarning("ItemSpawnSync plugin not found. Item sync will be disabled.");
            }
        }
        
        // HOST SIDE: Capture and get spawns
        public static string CaptureSpawns()
        {
            if (_itemSpawnPlugin == null) 
                return null;
            
            // Trigger all spawners (F4)
            SpawnerSyncManager.Instance.CaptureSpawns();
            
            // Get data as JSON
            string json = _itemSpawnPlugin.GetSpawnDataAsJson();
            
            return json;
        }
        
        // CLIENT SIDE: Apply spawns
        public static void ApplySpawns(string spawnJson)
        {
            if (_itemSpawnPlugin == null) 
                return;

            if (!string.IsNullOrEmpty(spawnJson))
            {
                // Load with lock to prevent accidental overwrites
                bool success = _itemSpawnPlugin.LoadSpawnDataFromJson(spawnJson, lockData: true);
                    
                if (success)
                {
                    Plugin.Logger.LogInfo("Loaded and locked item spawn data");
                }
                else
                {
                    Plugin.Logger.LogWarning("Spawn data already locked. Ignoring new data.");
                }
            }
        }
        
        // Optional: Save/Load from files
        public static void SaveCurrentSpawns()
        {
            _itemSpawnPlugin?.SaveCurrentSpawnData();
        }
        
        public static void LoadSpawnsFromFile(string filename = null)
        {
            _itemSpawnPlugin?.LoadSpawnDataFromFile(filename);
        }
    }
}

/*
 * USAGE IN YOUR MAIN PLUGIN:
 * 
 * In Plugin.cs Awake():
 * ItemSpawnSyncIntegration.Initialize();
 * 
 * When host wants to capture spawns:
 * ItemSpawnSyncIntegration.CaptureSpawns(wsPlayerClient);
 * 
 * When client connects:
 * ItemSpawnSyncIntegration.ApplySpawns(wsObserverClient);
 */
