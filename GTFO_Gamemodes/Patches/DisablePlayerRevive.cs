using HarmonyLib;
using Player;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.Setup))]
internal class DisablePlayerRevive
{
    public static readonly string PatchGroup = PatchGroups.NO_PLAYER_REVIVE;

    public static void Postfix(PlayerAgent __instance)
    {
        __instance.ReviveInteraction.gameObject.SetActive(false);
    }
}
