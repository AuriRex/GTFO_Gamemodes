using Enemies;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches
{
    //EGS_GuardsSpawn
    [HarmonyPatch(typeof(EGS_GuardsSpawn), nameof(EGS_GuardsSpawn.Update))]
    internal static class EGS_GuardsSpawn_Update_Patch
    {
        public static readonly string PatchGroup = PatchGroups.NO_SLEEPING_ENEMIES;

        public static bool Prefix()
        {
            return false;
        }
    }
}
