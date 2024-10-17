using HarmonyLib;
using Player;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.GetClipBulletsFromPack))]
internal class AmmoPatch
{
    public static readonly string PatchGroup = PatchGroups.INF_PLAYER_AMMO;

    public static bool Prefix(PlayerAmmoStorage __instance, AmmoType ammoType, ref int __result)
    {
        switch(ammoType)
        {
            case AmmoType.ResourcePackRel:
            case AmmoType.CurrentConsumable:
                return true;
        }

        InventorySlotAmmo ammoSlot = __instance.m_ammoStorage[(int)ammoType];
        __result = ammoSlot.BulletClipSize;

        __instance.UpdateAllAmmoUI();
        __instance.UpdateSlotAmmoUI(ammoSlot, __result);
        return false;
    }
}
