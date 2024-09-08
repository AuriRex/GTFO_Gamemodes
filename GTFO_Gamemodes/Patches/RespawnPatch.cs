using Enemies;
using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches
{
    [HarmonyPatch(typeof(EGS_GuardsRespawn), "Update")]
    internal static class EGS_GuardsRespawn_Update_Patch
    {
        public static readonly string PatchGroup = PatchGroups.NO_RESPAWN;

        public static bool Prefix()
        {
            return false;
        }
    }
}
