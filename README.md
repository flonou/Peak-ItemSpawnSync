# ItemSpawnSync - Standalone Plugin

A BepInEx plugin for synchronizing item spawns across clients in multiplayer games.



## Installation

1. Copy `ItemSpawnSync.dll` to `BepInEx/plugins/`
2. Launch game

## Configuration

After a first launch of the game with the mod installed, a configuration file will be create at: `BepInEx\config\ItemSpawnSync.cfg`

## Usage

### Standalone Usage (default keys)

1. (optional) Disabling spawning in the configuration file. This will prevent items to spawn automatically before saving and avoid confusion about what is saved and what isn't
2. **Press F4** - Triggers all spawners and record the spawn data
3. **Press F5** - Saves current spawn data to a file
4. **Press F6** - Loads spawn data from the file defined in the configuration and trigger spawners that should spawn on start

Files saved to: `BepInEx/config/ItemSpawnSync/MapName_spawn_data_date_time.json`
Default loaded file path: `BepInEx/config/ItemSpawnSync/spawn_data.json`

### Usage through other mods

See IntegrationExample.cs

## Data Structure

```json
{
  "Spawners": [
    {
      "SpawnerTypeName": "Luggage",
      "SpawnerInstanceID": 1,
      "SpawnerPosition": {"x": 10, "y": 0, "z": 5},
      "SpawnedItems": [
        {
          "SpawnSpotIndex": 0,
          "ItemPrefabName": "Flashlight",
          "Position": {"x": 10.1, "y": 0.5, "z": 5.2},
          "Rotation": {"x": 0, "y": 0, "z": 0, "w": 1},
          "PhotonViewID": 1001
        }
      ]
    }
  ]
}
```

## License

MIT
