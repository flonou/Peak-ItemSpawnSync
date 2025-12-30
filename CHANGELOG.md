# Changelog

## [0.9.1]

### Added

- ViewID of original item in SpawnedItemData
- OriginalIdToItemMap in SpawnerSyncManager to get the item spwaned by the plugin from its original viewID

### Changed

- Renamed Plugin.LoadSpawnData into Plugin.LoadSpawnDataInCurrentLevel
- Renamed SpawnerSyncManager.LoadMapSpawnerData into SpawnerSyncManager.LoadMapSpawnerDataInCurrentLevel
- Calling Plugin.LoadSpawnDataFromFile or Plugin.LoadSpawnDataFromJson no longer loads the spawner data in the current level. Call Plugin.LoadSpawnDataInCurrentLevel separately


## [0.9.0]
Initial version
