using System;
using System.Linq;
using Gamemodes.Net;
using Gear;
using Player;
using SNetwork;
using UnityEngine;

namespace Gamemodes.Core;

public static class GearUtils
{
    public static void EquipGear(GearIDRange gear, InventorySlot slot, bool refillGunsAndToolAmmo = false)
    {
        if (gear == null)
            return;

        var previousChecksum = CalculateCustomChecksum(PlayerBackpackManager.GetLocalItem(slot)?.GearIDRange);
        
        if (refillGunsAndToolAmmo)
            LocalReserveAmmoAction(AmmoType.Guns | AmmoType.Tool, AmmoAction.Fill);
        PlayerBackpackManager.EquipLocalGear(gear);
        GearManager.RegisterGearInSlotAsEquipped(gear.PlayfabItemInstanceId, slot);

        NetworkingManager.SendLocalPlayerGearChanged(gear.GetCustomChecksum(), previousChecksum, slot);
    }

    public static uint GetCustomChecksum(this GearIDRange gear)
    {
        return CalculateCustomChecksum(gear);
    }
    
    public static uint CalculateCustomChecksum(GearIDRange gear)
    {
        if (gear == null)
            return 0;
        
        var checksumGenerator = new ChecksumGenerator_32();
        
        foreach (var comp in gear.m_comps)
        {
            checksumGenerator.Insert(comp);
        }
        
        checksumGenerator.Insert("PlayfabItemName", gear.PlayfabItemName);
        
        return checksumGenerator.Checksum;
    }

    public static void LocalReserveAmmoAction(AmmoType actUpon, AmmoAction action, float? extraValue = null)
    {
        var localAmmo = PlayerBackpackManager.LocalBackpack.AmmoStorage;
        
        if (action == AmmoAction.None)
            return;

        bool modified = false;
        
        if (actUpon.HasFlag(AmmoType.Main))
            modified |= LocalReserveAmmoAction(localAmmo.StandardAmmo, action, extraValue);
            
        if (actUpon.HasFlag(AmmoType.Special))
            modified |= LocalReserveAmmoAction(localAmmo.SpecialAmmo, action, extraValue);
        
        if (actUpon.HasFlag(AmmoType.Tool))
            modified |= LocalReserveAmmoAction(localAmmo.ClassAmmo, action, extraValue);
        
        if (actUpon.HasFlag(AmmoType.Consumable))
            modified |= LocalReserveAmmoAction(localAmmo.ConsumableAmmo, action, extraValue);
        
        if (actUpon.HasFlag(AmmoType.ResourcePack))
            modified |= LocalReserveAmmoAction(localAmmo.ResourcePackAmmo, action, extraValue);

        localAmmo.NeedsSync |= modified;
    }

    private static bool LocalReserveAmmoAction(InventorySlotAmmo slot, AmmoAction action, float? extraValue = null)
    {
        float value = extraValue ?? 1f;
        
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
            case AmmoAction.SetToPercent:
                slot.AmmoInPack = slot.AmmoMaxCap * value;
                break;
            case AmmoAction.RemovePercent:
                slot.AmmoInPack -= slot.AmmoMaxCap * value;
                break;
            case AmmoAction.AddPercent:
                slot.AmmoInPack += slot.AmmoMaxCap * value;
                break;
            case AmmoAction.ClampToMinMax:
                if (slot.AmmoInPack > slot.AmmoMaxCap)
                {
                    slot.AmmoInPack = slot.AmmoMaxCap;
                    return true;
                }
                if (slot.AmmoInPack < 0)
                {
                    slot.AmmoInPack = 0;
                    return true;
                }
                return false;
        }
        return true;
    }

    public static void LocalGunClipAction(AmmoAction action, float? extraValue = null)
    {
        LocalGunClipAction(InventorySlot.GearStandard, action, extraValue);
        LocalGunClipAction(InventorySlot.GearSpecial, action, extraValue);
    }
    
    public static void LocalGunClipAction(InventorySlot slot, AmmoAction action, float? extraValue = null)
    {
        float value = extraValue ?? 1f;
        
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
            case AmmoAction.AddPercent:
                bulletWeapon.m_clip += (int) Mathf.Ceil(bulletWeapon.ClipSize * value);
                break;
            case AmmoAction.RemovePercent:
                bulletWeapon.m_clip -= (int) Mathf.Ceil(bulletWeapon.ClipSize * value);
                break;
            case AmmoAction.SetToPercent:
                bulletWeapon.m_clip = (int) Mathf.Ceil(bulletWeapon.ClipSize * value);
                break;
            case AmmoAction.ClampToMinMax:
                if (bulletWeapon.m_clip < 0)
                    bulletWeapon.m_clip = 0;
                if (bulletWeapon.m_clip > bulletWeapon.ClipSize)
                    bulletWeapon.m_clip = bulletWeapon.ClipSize;
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
        Empty,
        SetToPercent,
        RemovePercent,
        AddPercent,
        ClampToMinMax
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