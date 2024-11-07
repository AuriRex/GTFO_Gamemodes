using System;
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
    public static float DamageMax { get; set; } = 16f;
    public static float DamageBig { get; set; } = 6.5f;
    public static float DamageSmall { get; set; } = 3.25f;
    
    public static bool Prefix(LG_WeakDoorBladeDamage __instance, float dam, Agent sourceAgent, Vector3 position, float environmentMulti)
    {
        var player = sourceAgent.TryCast<PlayerAgent>();

        if (player == null)
            return true;
        
        NetworkingManager.GetPlayerInfo(player.Owner, out var info);

        if (info.Team != (int)GMTeam.Seekers)
            return true;
        
        Plugin.L.LogDebug($"Original: dam: {dam}, env: {environmentMulti}");
        
        /*
         * Uncharged:
         * Hammer : 3 dam,  3 env
         * Bat    : 3 dam,  5 env
         * Spear  : 2 dam,  2 env
         * Knife  : 2 dam, .8 env
         *
         * Fully Charged:
         * Hammer :   20 dam,   3 env
         * Bat    :   12 dam,   5 env
         * Spear  : 17.5 dam,   3 env
         * Knife  :  5.5 dam, 1.5 env
         */
        
        float damageNew = environmentMulti switch
        {
            < 1.75f when dam < 3f => DamageSmall, // Knife
            < 1.75f when dam >= 3f => dam * 1.19f, // Knife Half Charged
            >= 3f when dam >= 17.5f => DamageMax, // Hammer, Spear Full Charge
            >= 5f when dam >= 12f => DamageMax, // Full Charged Bat
            _ => DamageBig // Bat, Hammer, Spear
        };

        Plugin.L.LogDebug($"Overridden Weak Door Damage: {damageNew}");
        
        __instance.m_door.Cast<LG_WeakDoor>().m_sync.AttemptDoorInteraction(eDoorInteractionType.DoDamage, damageNew, 0f, position, null);
        return false;
    }
}