using Gamemodes.Net;
using Gear;
using HarmonyLib;
using HNS.Core;

namespace HNS.Patches;

[HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.OnWield))]
public class MeleePatch_Wield
{
    public static float SpeedMultiplierKnife = 1.15f;
    public static float SpeedMultiplierGeneric = 1.025f;
    
    public static void Postfix(MeleeWeaponFirstPerson __instance)
    {
        var isKnife = __instance.ArchetypeName == "Knife";

        NetworkingManager.GetPlayerInfo(__instance.Owner.Owner, out var info);

        var isNotHider = info.Team != (int)GMTeam.Hiders;

        var speedMulti = 1f;

        if (isNotHider)
        {
            speedMulti = SpeedMultiplierGeneric;

            if (isKnife)
            {
                speedMulti = SpeedMultiplierKnife;
            }
        }

        if (info.Team == (int)GMTeam.Camera)
            speedMulti = 1.25f;
        
        __instance.Owner.EnemyCollision.m_moveSpeedModifier = speedMulti;
    }
}

[HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.OnUnWield))]
public class MeleePatch_UnWield
{
    public static void Postfix(MeleeWeaponFirstPerson __instance)
    {
        __instance.Owner.EnemyCollision.m_moveSpeedModifier = 1f;
    }
}

[HarmonyPatch(typeof(PlayerEnemyCollision), nameof(PlayerEnemyCollision.LateUpdate))]
public static class PlayerEnemyCollision_Patch
{
    public static bool Prefix() => false;
}