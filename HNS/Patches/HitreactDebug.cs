/*
using HarmonyLib;
using SNetwork;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.Hitreact))]
public class Dam_PlayerDamageLocal__Hitreact__Patch
{
    public static void Postfix(Dam_PlayerDamageLocal __instance,
        float damage,
        Vector3 direction,
        bool triggerCameraShake,
        bool triggerGenericDialog,
        bool pushPlayer)
    {
        Plugin.L.LogWarning($"HitreactDebug Postfix:\ndamage: {damage}\ndirection: {direction.x}, {direction.y}, {direction.z}\ntriggerCameraShake: {triggerCameraShake}\ntriggerGenericDialog: {triggerGenericDialog}\npushPlayer: {pushPlayer}");
    }
}

[HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveMeleeDamage))]
public class Dam_PlayerDamageLocal__ReceiveMeleeDamage__Patch
{
    public static void Prefix()
    {
        Plugin.L.LogWarning($"Receiving Melee Damage!");
    }
}

[HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceivePushDamage))]
public class Dam_PlayerDamageLocal__ReceivePushDamage__Patch
{
    public static void Prefix()
    {
        Plugin.L.LogWarning($"Receiving Push Damage!");
    }
}

[HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.ReceiveSetHealth))]
public class Dam_PlayerDamageLocal__ReceiveSetHealth__Patch
{
    public static void Prefix(Dam_PlayerDamageLocal __instance, pSetHealthData data)
    {
        Plugin.L.LogWarning($"Receiving Set Health! {data.health.Get(__instance.HealthMax)}");
    }
}
*/
