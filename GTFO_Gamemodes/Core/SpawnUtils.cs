using System;
using AIGraph;
using CullingSystem;
using GameData;
using LevelGeneration;
using Player;
using UnityEngine;

namespace Gamemodes.Core;

public static class SpawnUtils
{
    // TODO: This essentially breaks late joining if used (=> an item is spawned)
    internal class SNetReplicationSelfManagedOverride : IDisposable
    {
        public SNetReplicationSelfManagedOverride()
        {
            Patches.Required.ReplicationPatch.OverrideCount++;
        }
        
        public void Dispose()
        {
            Patches.Required.ReplicationPatch.OverrideCount--;
        }
    }
    
    internal static bool SpawnItemAndPickUp(uint itemId, PlayerAgent player, float ammoMultiplier = 1f, bool switchToNewItem = false)
    {
        var wieldedSlot = player?.Sync?.GetWieldedSlot() ?? InventorySlot.GearMelee;

        if (!SpawnItemAtPlayerPos(itemId, player, out var item, ammoMultiplier))
            return false;

        if (!player!.IsLocallyOwned)
            return true;
        
        item.PickupInteraction.Cast<Interact_Pickup_PickupItem>().TriggerInteractionAction(player);

        wieldedSlot = switchToNewItem ? item.ItemDataBlock.inventorySlot : wieldedSlot;
        
        player!.Sync!.WantsToWieldSlot(wieldedSlot);

        return true;
    }

    internal static bool SpawnItemAtPlayerPos(uint itemId, PlayerAgent player, out Item item, float ammoMultiplier = 1f)
    {
        item = null;
        if (player == null)
            return false;
        
        return SpawnItemLocally(itemId, player.CourseNode, player.Position, out item, ammoMultiplier);
    }
    
    internal static bool SpawnItemLocally(uint itemId, out Item item, float ammoMultiplier = 1f)
    {
        return SpawnItemLocally(itemId, null, null, out item, ammoMultiplier);
    }

    internal static bool SpawnItemLocally(uint itemId, AIG_CourseNode node, Vector3? position, out Item item, float ammoMultiplier = 1f)
    {
        item = null;
        float ammo;
        var block = ItemDataBlock.GetBlock(itemId);
        if (block == null)
        {
            return false;
        }

        var inventorySlot = block.inventorySlot;
        
        switch (inventorySlot)
        {
            case InventorySlot.Consumable:
                ammo = block.ConsumableAmmoMax;
                break;

            case InventorySlot.ResourcePack:
                ammo = 120;
                break;
            
            case InventorySlot.InLevelCarry:
                ammo = 1f;
                break;
            
            default:
                return false;
        }

        ammo *= ammoMultiplier;
        
        pItemData data = new()
        {
            itemID_gearCRC = itemId
        };
        data.custom.ammo = ammo;

        node ??= Builder.CurrentFloor.MainDimension.GetStartCourseNode();
        
        data.originCourseNode.Set(node);
        
        using (new SNetReplicationSelfManagedOverride())
        {
            item = ItemSpawnManager.SpawnItem(itemId, ItemMode.Pickup, Vector3.zero, Quaternion.identity, true, data, node.m_area.transform);
        }

        item.transform.position = position ?? node.GetRandomPositionInside();

        if (inventorySlot == InventorySlot.InLevelCarry)
        {
            var pickupCore = item.GetComponent<CarryItemPickup_Core>();
            
            if (pickupCore != null)
                pickupCore.SpawnNode = node;
        }
        
        CreateCuller(item);

        return true;
    }

    private static void CreateCuller(Item item)
    {
        var sro = item.gameObject.GetComponent<C_SpecialRenderableObject>();

        var culler = new C_CullerSpecialRenderableObject(sro);
            
        culler.Bounds = sro.GetBounds();
        culler.RegisterInNodes();
        culler.Hide();
        
        sro.OnFactoryCullingSetupDone(culler.GetMainNode());
    }

    public static class Consumables
    {
        public const uint LONG_RANGE_FLASHLIGHT = 30;
        public const uint CFOAM_TRIPMINE = 144;
        public const uint EXPLOSIVE_TRIPMINE = 139;
        public const uint FOGREPELLER = 117;
        public const uint LOCKMELTER = 116;
        public const uint CFOAM_GRENADE = 115;
        public const uint MELEE_SYRINGE = 142;
        public const uint MEDI_SYRINGE = 140;
        public const uint GLOWSTICKS_YELLOW = 174;
        public const uint GLOWSTICKS_ORANGE = 167;
        public const uint GLOWSTICKS_RED = 130;
        public const uint GLOWSTICKS_GREEN = 114;
    }

    public static class ResourcePacks
    {
        public const uint MEDI = 102;
        public const uint TOOL = 127;
        public const uint AMMO = 101;
        public const uint DISINFECT = 132;
    }

    public static class InLevelCarry
    {
        public const uint MATTER_WAVE_PROJECTOR = 164;
        public const uint POWER_CELL = 131;
        public const uint DATA_SPHERE = 151;
    }
}