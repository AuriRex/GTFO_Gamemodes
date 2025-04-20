using Gamemodes.Net;
using HarmonyLib;
using HNS.Core;
using Player;
using UnityEngine;

namespace HNS.Patches;

//private static PlayerAgent CheckForPlayerTarget(ArchetypeDataBlock archetypeData, Transform detectionSource, GameObject previousTarget)
[HarmonyPatch(typeof(SentryGunInstance_Detection), nameof(SentryGunInstance_Detection.CheckForPlayerTarget))]
internal class SentryTargetingPatch
{
    private const float SENTRY_TARGET_MAX_DISTANCE = 15f;
    private const float SENTRY_FIRING_DELAY = 0.2f;
    
    public static void Postfix(Transform detectionSource, ref PlayerAgent __result)
    {
        if (__result == null)
            return;

        NetworkingManager.GetPlayerInfo(__result.Owner, out var info);

        if (info.Team != (int)GMTeam.Hiders)
            __result = null;

        var detectionInstance = detectionSource.GetComponentInParent<SentryGunInstance_Detection>();

        if (detectionInstance == null)
            return;
        
        var distanceToPlayer = Vector3.Distance(detectionInstance.gameObject.transform.position, info.PlayerAgent.transform.position);
        
        if (distanceToPlayer >= SENTRY_TARGET_MAX_DISTANCE)
            __result = null;

        if (__result == null)
            return;
        
        // We intentionally return null here and call the event ourselves to modify m_startFireTimer right after
        __result = null;
        
        detectionInstance.Target = info.PlayerAgent.gameObject;
        detectionInstance.TargetAimTrans = info.PlayerAgent.TentacleTarget;

        if (!detectionInstance.HasTarget)
        {
            detectionInstance.HasTarget = true;
        
            detectionInstance.OnFoundTarget?.Invoke();
            var sentry = detectionInstance.m_core.TryCast<SentryGunInstance>();
            if (sentry != null)
            {
                sentry.m_startFireTimer = Clock.Time + SENTRY_FIRING_DELAY;
            }
        }
    }
}
