using Agents;
using Gamemodes.Net;
using HarmonyLib;
using HNS.Core;
using LevelGeneration;
using Player;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(LG_WeakDoorBladeDamage), nameof(LG_WeakDoorBladeDamage.MeleeDamage))]
public class DoorPatch
{
    public static bool Prefix(LG_WeakDoorBladeDamage __instance, float dam, Agent sourceAgent, Vector3 position, float environmentMulti)
    {
        var player = sourceAgent.TryCast<PlayerAgent>();

        if (player == null)
            return true;
        
        NetworkingManager.GetPlayerInfo(player.Owner, out var info);

        if (info.Team != (int)GMTeam.Seekers)
            return true;
        
        float damageNew = 10;
        if (dam < 15f && environmentMulti < 1f)
        {
            damageNew = 5;
        }
        
        Plugin.L.LogDebug($"Overridden Weak Door Damage: {damageNew}");
        
        __instance.m_door.Cast<LG_WeakDoor>().m_sync.AttemptDoorInteraction(eDoorInteractionType.DoDamage, damageNew, 0f, position, null);
        return false;
    }
}