using HarmonyLib;
using SNetwork;

namespace HNS.Patches;

[HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveMeleeDamage))]
public class Dam_PlayerDamageBase__ReceiveMeleeDamage__Patch
{
    public static void Prefix(Dam_PlayerDamageBase __instance, pFullDamageData data)
    {
        if (!SNet.IsMaster)
            return;

        var owner = __instance.Owner.Owner;

        if (owner.IsMaster)
            return;
        
        __instance.m_meleeDamagePacket.Send(data, SNet_ChannelType.GameOrderCritical, owner);
    }
}