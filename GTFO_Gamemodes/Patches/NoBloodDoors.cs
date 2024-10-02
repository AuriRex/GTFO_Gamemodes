using HarmonyLib;
using LevelGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

// public void SetupActiveEnemyWaveData(ActiveEnemyWaveData enemyWaveData)
[HarmonyPatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.SetupActiveEnemyWaveData))]
internal class NoBloodDoors
{
    public static readonly string PatchGroup = PatchGroups.NO_BLOOD_DOORS;

    public static bool Prefix()
    {
        return false;
    }
}
