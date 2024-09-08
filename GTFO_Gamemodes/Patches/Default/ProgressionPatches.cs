using BoosterImplants;
using GameData;
using HarmonyLib;

namespace Gamemodes.Patches.Default
{
    // Prevent progression thingies, Default patch group
    [HarmonyWrapSafe]
    [HarmonyPatch(typeof(RundownManager), nameof(RundownManager.NewGameSession))]
    public class RundownManager_NewGameSession_Patch
    {
        public static bool Prefix()
        {
            Plugin.L.LogDebug("New Game Session has started!");
            GameEvents.InvokeOnGameSessionStart();
            return false;
        }
    }

    [HarmonyWrapSafe]
    [HarmonyPatch(typeof(RundownManager), nameof(RundownManager.OnExpeditionEnded))]
    public class RundownManager_EndGameSession_Patch
    {
        public static bool Prefix(ExpeditionEndState endState)
        {
            var artifactInventory = BoosterImplantManager.ArtifactInventory;
            (int Muted, int Bold, int Aggressive) artifacts = (artifactInventory.GetArtifactCount(ArtifactCategory.Common), artifactInventory.GetArtifactCount(ArtifactCategory.Uncommon), artifactInventory.GetArtifactCount(ArtifactCategory.Rare));
            Plugin.L.LogDebug("Game Session has ended!");
            GameEvents.InvokeOnGameSessionEnd(endState, artifacts);
            return false;
        }
    }
}
