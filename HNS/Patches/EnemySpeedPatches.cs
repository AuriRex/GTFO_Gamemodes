using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using Gamemodes.Core;
using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
public class EnemyAgent__Setup__Patch
{
    public const float SPEED_MOVEMENT_MAX = 20f;
    public const float ACCELERATION = 30f;
    
    public static void Postfix(EnemyAgent __instance)
    {
        CoroutineManager.StartCoroutine(Coroutines.DoAfter(1f, () =>
        {
            //Plugin.L.LogDebug($"Doing the sus thing: {__instance.name}");
            if (__instance == null || __instance.m_isBeingDestroyed || !__instance.m_alive)
                return;
            
            //Plugin.L.LogDebug("Yippie!");
            __instance.Anim.speed = 3; // 2.4 is default for striker_child
            
            var loco = __instance.Locomotion;
            
            loco.m_maxMovementSpeed = SPEED_MOVEMENT_MAX; // 5 for striker_child
            loco.PathMove.m_defaultAcceleration = ACCELERATION; // 8 for striker_child
        }).WrapToIl2Cpp());
        
            
        
    }
}

[HarmonyPatch(typeof(EnemyLocomotion), nameof(EnemyLocomotion.SetRunMoveSpeed))]
public class EnemyLocomotion__SetRunMoveSpeed__Patch
{
    public static void Postfix(EnemyLocomotion __instance)
    {
        __instance.m_maxMovementSpeed = EnemyAgent__Setup__Patch.SPEED_MOVEMENT_MAX;
        __instance.PathMove.m_defaultAcceleration = EnemyAgent__Setup__Patch.ACCELERATION;
    }
}

[HarmonyPatch(typeof(EnemyLocomotion), nameof(EnemyLocomotion.SetTooHurtToRun))]
public class EnemyLocomotion__SetTooHurtToRun__Patch
{
    public static void Postfix(EnemyLocomotion __instance)
    {
        __instance.m_maxMovementSpeed = EnemyAgent__Setup__Patch.SPEED_MOVEMENT_MAX;
        __instance.PathMove.m_defaultAcceleration = EnemyAgent__Setup__Patch.ACCELERATION;
    }
}