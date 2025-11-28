using Gear;
using HarmonyLib;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(MeleeWeaponFirstPerson), nameof(MeleeWeaponFirstPerson.UpdateLocal))]
public class MeleeIndicatorPatch
{
    public static void Postfix(MeleeWeaponFirstPerson __instance)
    {
        var fpsCam = __instance.Owner.FPSCamera;
        
        if (fpsCam.CameraRayObject != null
            && fpsCam.CameraRayDist <= __instance.MeleeArchetypeData.CameraDamageRayLength
            && (fpsCam.CameraRayObject.layer == LayerManager.LAYER_PLAYER_SYNCED || fpsCam.CameraRayObject.layer == LayerManager.LAYER_ENEMY_DAMAGABLE))
        {
            if (__instance.m_lookAtEnemy)
            {
                return;
            }

            GuiManager.CrosshairLayer.ScaleToSize(__instance.HipFireCrosshairSize * 0.6f);
            GuiManager.CrosshairLayer.TriggerBlink(Color.white);
            __instance.m_lookAtEnemy = true;
            return;
        }

        if (!__instance.m_lookAtEnemy)
        {
            return;
        }

        GuiManager.CrosshairLayer.ScaleToSize(__instance.HipFireCrosshairSize);
        GuiManager.CrosshairLayer.ResetChargeUpColor();
        __instance.m_lookAtEnemy = false;
    }
}