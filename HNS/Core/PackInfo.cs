using AIGraph;
using Gear;
using LevelGeneration;
using UnityEngine;

namespace HNS.Core;

public class PackInfo
{
    public ResourcePackPickup pickup;

    public Vector3 position;
    public AIG_CourseNode node;
    public LG_ResourceContainer_Storage container;

    private bool _lateSetup;
        
    public void LateSetup()
    {
        if (_lateSetup)
            return;

        _lateSetup = true;
            
        container ??= pickup.gameObject.GetComponentInParent<LG_ResourceContainer_Storage>();
        node ??= container.m_core.SpawnNode;
            
        foreach (var slot in container.m_storageSlots)
        {
            if (Vector3.Distance(slot.ResourcePack.position, position) > 0.05f)
                continue;
                
            position = slot.Consumable.position;
            break;
        }
    }
}