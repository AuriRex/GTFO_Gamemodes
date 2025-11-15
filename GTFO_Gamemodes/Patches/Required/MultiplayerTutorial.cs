using System.Linq;
using CellMenu;
using GameData;
using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.Setup))]
public static class MultiplayerTutorial
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;

    private const uint TUTORIAL_PID = 39;
    
    public static void Postfix()
    {
        // Datablocks ready

        var tutorialRundown = RundownDataBlock.GetAllBlocks().ToArray().FirstOrDefault(block => block.persistentID == TUTORIAL_PID);

        if (tutorialRundown == null)
        {
            Plugin.L.LogWarning("Tutorial rundown is null :(");
            return;
        }

        foreach (var level in tutorialRundown.TierA)
        {
            level.Enabled = true;

            level.IsSinglePlayer = false;
            level.SkipLobby = false;
            
            level.UseGearPicker = false;
        }
    }
}

[HarmonyPatch(typeof(CM_PageRundown_New), nameof(CM_PageRundown_New.OnEnable))]
public static class MultiplayerTutorial_ForceEnableButton
{
    public static void Postfix(CM_PageRundown_New __instance)
    {
        __instance.m_tutorialButton.SetVisible(true);
    }
}