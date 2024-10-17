using Gamemodes.Net;
using HarmonyLib;
using HNS.Core;
using Player;

namespace HNS.Patches;

//private static PlayerAgent CheckForPlayerTarget(ArchetypeDataBlock archetypeData, Transform detectionSource, GameObject previousTarget)
[HarmonyPatch(typeof(SentryGunInstance_Detection), nameof(SentryGunInstance_Detection.CheckForPlayerTarget))]
internal class SentryTargetingPatch
{
    public static void Postfix(ref PlayerAgent __result)
    {
        if (__result == null)
            return;

        NetworkingManager.GetPlayerInfo(__result.Owner, out var info);

        if (info.Team != (int)GMTeam.Hiders)
            __result = null;
    }
}
