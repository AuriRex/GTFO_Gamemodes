using System;
using Gamemodes.Net;
using Gear;
using Player;
using SNetwork;

namespace Gamemodes.Core;

public static class GearUtils
{
    public static void EquipGear(GearIDRange gear, InventorySlot slot, bool refillGunsAndToolAmmo = false)
    {
        var previousChecksum = PlayerBackpackManager.GetLocalItem(slot)?.GearIDRange?.m_checksum ?? 0;
        
        if (refillGunsAndToolAmmo)
            LocalAmmoAction(AmmoType.Guns | AmmoType.Tool, AmmoAction.Fill);
        PlayerBackpackManager.EquipLocalGear(gear);
        GearManager.RegisterGearInSlotAsEquipped(gear.PlayfabItemInstanceId, slot);

        NetworkingManager.SendLocalPlayerGearChanged(gear.m_checksum, previousChecksum, slot);
    }

    public static void LocalAmmoAction(AmmoType actUpon, AmmoAction action)
    {
        var localAmmo = PlayerBackpackManager.LocalBackpack.AmmoStorage;
        
        if (action == AmmoAction.None)
            return;

        bool modified = false;
        
        if (actUpon.HasFlag(AmmoType.Main))
            modified |= LocalAmmoAction(localAmmo.StandardAmmo, action);
            
        if (actUpon.HasFlag(AmmoType.Special))
            modified |= LocalAmmoAction(localAmmo.SpecialAmmo, action);
        
        if (actUpon.HasFlag(AmmoType.Tool))
            modified |= LocalAmmoAction(localAmmo.ClassAmmo, action);
        
        if (actUpon.HasFlag(AmmoType.Consumable))
            modified |= LocalAmmoAction(localAmmo.ConsumableAmmo, action);
        
        if (actUpon.HasFlag(AmmoType.ResourcePack))
            modified |= LocalAmmoAction(localAmmo.ResourcePackAmmo, action);

        localAmmo.NeedsSync |= modified;
    }

    private static bool LocalAmmoAction(InventorySlotAmmo slot, AmmoAction action)
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

        var mines = UnityEngine.Object.FindObjectsOfType<MineDeployerInstance>();
        
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
        
        var cfoamBlobs = UnityEngine.Object.FindObjectsOfType<GlueGunProjectile>();
        
        foreach (var blob in cfoamBlobs)
        {
            if (blob.m_owner != player)
                continue;
            
            ProjectileManager.WantToDestroyGlue(blob.SyncID);
        }
    }
}