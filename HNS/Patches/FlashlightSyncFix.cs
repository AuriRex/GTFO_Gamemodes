using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Player;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.WantsToSetFlashlightEnabled))]
public class PlayerSync__WantsToSetFlashlightEnabled__Patch
{
    private const float ATTEMPTED_FLASHLIGHT_SYNC_TIME = 5f;
    
    private static Coroutine _flashSyncRoutine;
    
    public static void Postfix(PlayerSync __instance, bool enable)
    {
        if (!__instance.m_agent.IsLocallyOwned)
            return;

        TryStopRoutine(__instance);

        _flashSyncRoutine = __instance.StartCoroutine(FlashlightSyncRoutine(__instance).WrapToIl2Cpp());
        
        //Plugin.L.LogWarning($"{nameof(PlayerSync__WantsToSetFlashlightEnabled__Patch)} POSTFIX: enable:{enable}");
    }

    private static void TryStopRoutine(PlayerSync routineHost)
    {
        if (_flashSyncRoutine == null)
            return;

        routineHost.StopCoroutine(_flashSyncRoutine);
        _flashSyncRoutine = null;
    }

    private static IEnumerator FlashlightSyncRoutine(PlayerSync sync)
    {
        yield return new WaitForSeconds(ATTEMPTED_FLASHLIGHT_SYNC_TIME);

        if (sync == null)
            yield break;
        
        var agent = sync.m_agent;

        if (agent == null || agent.IsBeingDespawned || agent.IsBeingDestroyed)
            yield break;
        
        Plugin.L.LogDebug("Attempting to sync flashlight state ...");
        // Should network the current flashlight state again
        sync.SyncInventory(syncBackPack: false);

        _flashSyncRoutine = null;
    }
}