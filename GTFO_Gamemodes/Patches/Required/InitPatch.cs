using GameData;
using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyWrapSafe]
[HarmonyPatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
internal class GameDataInit_Initialize_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;

    private static bool _hasInited = false;
    public static void Postfix()
    {
        if (_hasInited)
            return;

        _hasInited = true;

        GameEvents.InvokeOnGameDataInit();
    }
}
