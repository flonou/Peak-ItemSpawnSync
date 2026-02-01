
using System;
using System.Collections.Generic;
using System.Linq;

using Zorro.Core;


namespace ItemSpawnSync.Data;

public class LootDataContainer
{

    protected static string LootDataFileName = "LootData.json";

    public static Dictionary<string, Dictionary<string, int>> SpawnData = new();


    public static void ImportLootData()
    {
        string filePath = System.IO.Path.Combine(Plugin.DataDirectory, LootDataFileName);
        if (!System.IO.File.Exists(filePath))
        {
            Plugin.Log?.LogError($"Loot data file not found at {filePath}");
            return;
        }

        string json = System.IO.File.ReadAllText(filePath);
        SpawnData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json) 
            ?? new Dictionary<string, Dictionary<string, int>>();

        Plugin.Log?.LogInfo($"Loot data imported from {filePath}");
        ApplyLootData();
    }

    public static void ExportLootData()
    {
        ExtractLootData();
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(SpawnData, Newtonsoft.Json.Formatting.Indented);
        
        string filePath = System.IO.Path.Combine(Plugin.DataDirectory, LootDataFileName);
        System.IO.File.WriteAllText(filePath, json);
        Plugin.Log?.LogInfo($"Loot data exported to {filePath}");
    }

    private static void ApplyLootData()
    {
        if (LootData.AllSpawnWeightData == null)
		{
    	    LootData.PopulateLootData();
		}
        if (LootData.AllSpawnWeightData == null)
        {
            Plugin.Log?.LogError("No loot data found to export.");
            return;
        }
        foreach (KeyValuePair<string, Dictionary<string, int>> importedPoolSpawnData in SpawnData)
        {
            string poolName = importedPoolSpawnData.Key;
            foreach (SpawnPool pool in LootData.AllSpawnWeightData.Keys)
            {
                if (pool.ToString() == poolName)
                {
                    foreach (var itemsNamesSpawnChance in importedPoolSpawnData.Value)
                    {
                        string itemName = itemsNamesSpawnChance.Key;
                        int spawnWeight = itemsNamesSpawnChance.Value;
                        // Find item by name, delete existing entry, and add new one with updated spawn weight
                        var itemLookupEntry = SingletonAsset<ItemDatabase>.Instance.itemLookup.FirstOrDefault(kvp =>
                        {
                            if (ItemDatabase.TryGetItem(kvp.Key, out var item))
                            {
                                return item.gameObject.name == itemName;
                            }
                            return false;
                        });
                        if (LootData.AllSpawnWeightData[pool].ContainsKey(itemLookupEntry.Key))
                        {
                            LootData.AllSpawnWeightData[pool].Remove(itemLookupEntry.Key);
                        }
                        if (spawnWeight > 0)
                        {
                            LootData.AllSpawnWeightData[pool].Add(itemLookupEntry.Key, spawnWeight);                        
                        } else
                        {
                            Plugin.Log?.LogInfo($"Skipping item {itemName} with zero spawn weight in pool {poolName}");
                        }
                    }
                    break;
                }
            }
        }
    }


    private static void ExtractLootData()
    {
        if (LootData.AllSpawnWeightData == null)
		{
    	    LootData.PopulateLootData();
		}
        if (LootData.AllSpawnWeightData == null)
        {
            Plugin.Log?.LogError("No loot data found to export.");
            return;
        }
        // export loot data to json text file
        foreach (var kvp in LootData.AllSpawnWeightData)
        {
            SpawnPool spawnPool = kvp.Key;
            string poolName = spawnPool.ToString();
            Dictionary<string, int> currentPoolData = new();
            SpawnData.Add(poolName, currentPoolData);
            foreach (KeyValuePair<ushort, int> idRarity in kvp.Value)
			{
				if (ItemDatabase.TryGetItem(idRarity.Key, out var item))
				{
					LootData component = item.GetComponent<LootData>();
                    int spawnWeight = idRarity.Value;
                    currentPoolData.Add(item.gameObject.name, spawnWeight);
				}
			}
        }
    }
}
