using HarmonyLib;

using Photon.Pun;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

namespace FFSPeak.Patches;

/// <summary>
/// Transpiler patch for RespawnChest.SpawnItems that captures and returns
/// the result from base.SpawnItems(spawnSpots) instead of discarding it.
///
/// The original override calls base.SpawnItems but ignores its return value
/// (IL: call + pop), always returning an empty list. The transpiler replaces
/// the pop with stloc_0 so the base result flows through to the return.
/// </summary>
[HarmonyPatch(typeof(RespawnChest), nameof(RespawnChest.SpawnItems))]
public class RespawnChestSpawnItemsPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        MethodInfo baseSpawnItemsMethod = typeof(Spawner).GetMethod(
            "SpawnItems",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(List<Transform>) },
            null);

        for (int i = 0; i < codes.Count - 1; i++)
        {
            bool isBaseCall = codes[i].opcode == OpCodes.Call
                && codes[i].operand is MethodInfo mi
                && mi == baseSpawnItemsMethod;

            if (isBaseCall && codes[i + 1].opcode == OpCodes.Pop)
            {
                // Replace the `pop` (discarding the base return value) with
                // `stloc.0` to store it back into the `result` local variable,
                // which is then returned at the end of the method.
                codes[i + 1] = new CodeInstruction(OpCodes.Stloc_0);
                break;
            }
        }

        return codes;
    }
}
