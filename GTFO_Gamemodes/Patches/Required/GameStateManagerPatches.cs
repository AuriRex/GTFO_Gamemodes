using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required
{
    [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.DoChangeState))]
    internal static class GameStateManagerPatches
    {
        public static readonly string PatchGroup = PatchGroups.REQUIRED;

        public static void Postfix(eGameStateName nextState)
        {
            GameEvents.InvokeOnGameStateChanged(nextState);
        }
    }
}
