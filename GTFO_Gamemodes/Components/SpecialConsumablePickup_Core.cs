using Il2CppInterop.Runtime.Injection;
using LevelGeneration;
using Player;

namespace Gamemodes.Components;

public class SpecialConsumablePickup_Core : ConsumablePickup_Core
{
    public static SpecialConsumablePickup_Core TransformOriginal(ConsumablePickup_Core og)
    {
        var syncComp = og.m_syncComp;
        var interactComp = og.m_interactComp;
        var gameObject = og.gameObject;
        
        UnityEngine.Object.Destroy(og);

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SpecialConsumablePickup_Core>())
            ClassInjector.RegisterTypeInIl2Cpp<SpecialConsumablePickup_Core>();
        
        var comp = gameObject.AddComponent<SpecialConsumablePickup_Core>();
        
        comp.m_syncComp = syncComp;
        comp.m_interactComp = interactComp;

        return comp;
    }
    
    public override pItemData Get_pItemData()
    {
        var itemData = this.pItemData;
        itemData.itemID_gearCRC = base.ItemDataBlock.persistentID;
        itemData.replicatorRef.SetID((new iPickupItemSync(this.m_syncComp.Pointer)).GetReplicator());
        itemData.slot = this.ItemDataBlock.inventorySlot;
        itemData.custom = this.GetCustomData();
        
        this.pItemData = itemData;
        return itemData;
    }
}