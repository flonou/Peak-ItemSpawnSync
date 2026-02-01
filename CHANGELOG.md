# Changelog

## [0.9.2]
- Added KeyPressed options to export and import all spawn data into an external file. This allows to change the spawnable items and their spawn weights
- Changed key bindings to handle modifiers. Default keybindings now include the left contol key modifier

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
