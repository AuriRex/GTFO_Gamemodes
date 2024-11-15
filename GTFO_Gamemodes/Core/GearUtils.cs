using System;
using System.Linq;
using Gamemodes.Net;
using Gear;
using Player;
using SNetwork;

namespace Gamemodes.Core;

public static class GearUtils
{
    public static void EquipGear(GearIDRange gear, InventorySlot slot, bool refillGunsAndToolAmmo = false)
    {
        if (gear == null)
            return;
        
        var previousChecksum = PlayerBackpackManager.GetLocalItem(slot)?.GearIDRange?.m_checksum ?? 0;
        
        if (refillGunsAndToolAmmo)
            LocalReserveAmmoAction(AmmoType.Guns | AmmoType.Tool, AmmoAction.Fill);
        PlayerBackpackManager.EquipLocalGear(gear);
        GearManager.RegisterGearInSlotAsEquipped(gear.PlayfabItemInstanceId, slot);

        NetworkingManager.SendLocalPlayerGearChanged(gear.m_checksum, previousChecksum, slot);
    }

    public static void LocalReserveAmmoAction(AmmoType actUpon, AmmoAction action)
    {
        var localAmmo = PlayerBackpackManager.LocalBackpack.AmmoStorage;
        
        if (action == AmmoAction.None)
            return;

        bool modified = false;
        
        if (actUpon.HasFlag(AmmoType.Main))
            modified |= LocalReserveAmmoAction(localAmmo.StandardAmmo, action);
            
        if (actUpon.HasFlag(AmmoType.Special))
            modified |= LocalReserveAmmoAction(localAmmo.SpecialAmmo, action);
        
        if (actUpon.HasFlag(AmmoType.Tool))
            modified |= LocalReserveAmmoAction(localAmmo.ClassAmmo, action);
        
        if (actUpon.HasFlag(AmmoType.Consumable))
            modified |= LocalReserveAmmoAction(localAmmo.ConsumableAmmo, action);
        
        if (actUpon.HasFlag(AmmoType.ResourcePack))
            modified |= LocalReserveAmmoAction(localAmmo.ResourcePackAmmo, action);

        localAmmo.NeedsSync |= modified;
    }

    private static bool LocalReserveAmmoAction(InventorySlotAmmo slot, AmmoAction action)
    {
        switch (action)
        {
            default:
            case AmmoAction.None:
            case AmmoAction.Keep:
                return false;
            case AmmoAction.Empty:
                if (slot.AmmoInPack <= 0)
                    return false;
                slot.AmmoInPack = 0;
                break;
            case AmmoAction.Fill:
                if (slot.AmmoInPack >= slot.AmmoMaxCap)
                    return false;
                slot.AmmoInPack = slot.AmmoMaxCap;
                break;
        }
        return true;
    }

    public static void LocalGunClipAction(AmmoAction action)
    {
        LocalGunClipAction(InventorySlot.GearStandard, action);
        LocalGunClipAction(InventorySlot.GearSpecial, action);
    }
    
    public static void LocalGunClipAction(InventorySlot slot, AmmoAction action)
    {
        var ammoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
        InventorySlotAmmo slotAmmo;
        switch (slot)
        {
            default:
                return;
            case InventorySlot.GearStandard:
                slotAmmo = ammoStorage.StandardAmmo;
                break;
            case InventorySlot.GearSpecial:
                slotAmmo = ammoStorage.SpecialAmmo;
                break;
        }
        
        if (!PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(slot, out var backpackItem))
            return;

        var bulletWeapon = backpackItem.Instance.TryCast<BulletWeapon>();

        if (bulletWeapon == null)
            return;

        switch (action)
        {
            default:
            case AmmoAction.Keep:
            case AmmoAction.None:
                break;
            case AmmoAction.Fill:
                bulletWeapon.m_clip = bulletWeapon.ClipSize;
                break;
            case AmmoAction.Empty:
                bulletWeapon.m_clip = 0;
                break;
        }

        ammoStorage.UpdateSlotAmmoUI(slotAmmo, bulletWeapon.m_clip);
    }
    
    [Flags]
    public enum AmmoType
    {
        Main = 1 << 0,
        Special = 1 << 1,
        Tool = 1 << 2,
        Consumable = 1 << 3,
        ResourcePack = 1 << 4,
        
        Guns = Main | Special,
        All = Main | Special | Tool | Consumable | ResourcePack,
    }

    [Flags]
    public enum AmmoAction
    {
        None,
        Keep,
        Fill,
        Empty
    }

    public static void CleanupMinesForPlayer(PlayerAgent player)
    {
        if (!SNet.IsMaster)
            return;

        var mines = ToolInstanceCaches.MineCache.All;
        
        foreach (var mine in mines)
        {
            if (mine.Owner != player)
                continue;
            
            ItemReplicationManager.DeSpawn(mine.Replicator);
        }
    }

    public static void CleanupGlueBlobsForPlayer(PlayerAgent player)
    {
        if (!SNet.IsMaster)
            return;

        var cfoamBlobs = ToolInstanceCaches.GlueCache.All;
        
        foreach (var blob in cfoamBlobs)
        {
            if (blob.m_owner != player)
                continue;
            
            ProjectileManager.WantToDestroyGlue(blob.SyncID);
        }
    }

    public static void LocalTryPickupDeployedSentry()
    {
        if (!NetworkingManager.InLevel || !PlayerBackpackManager.LocalBackpack.IsDeployed(InventorySlot.GearClass))
        {
            return;
        }

        var sentries = ToolInstanceCaches.SentryCache.All;

        foreach (var sentry in sentries.Where(s => s.m_belongsToBackpack.IsLocal))
        {
            sentry.m_interactPickup.TriggerInteractionAction(PlayerManager.GetLocalPlayerAgent());
        }
    }
}