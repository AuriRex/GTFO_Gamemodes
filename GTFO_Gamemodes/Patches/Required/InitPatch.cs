using CellMenu;
using GameData;
using Globals;
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

[HarmonyWrapSafe]
[HarmonyPatch(typeof(ItemSpawnManager), nameof(ItemSpawnManager.SetupItemPrefabs))]
internal class ItemSpawnManager_SetupItemPrefabs_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix()
    {
        GameEvents.InvokeOnItemPrefabsSetup();
    }
}