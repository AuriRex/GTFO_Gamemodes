using System;
using System.Collections.Generic;
using System.Linq;
using Gear;
using Player;
using SNetwork;
using UnityEngine;

namespace Gamemodes.UI.Menu;

public class CustomGearSelector
{
    private readonly SelectionPopupMenu _menu;
    private readonly GearIDRange[] _gear;
    private readonly InventorySlot _slot;
    
    public CustomGearSelector(IEnumerable<GearIDRange> availableGear, InventorySlot slot, string title = "Gear Selector")
    {
        _gear = availableGear.ToArray();
        _slot = slot;
        
        _menu = new SelectionPopupMenu();

        var header = new SelectionPopupHeader(title);

        header.PreDraw = self =>
        {
            if (!TryGetItemFromBP(SNet.LocalPlayer, out var item))
            {
                return;
            }

            self.ActiveItemID = item.GearIDRange.PlayfabItemId;
            self.LastSelectedItemID = item.GearIDRange.PlayfabItemId;
        };

        foreach (var gear in _gear)
        {
            header.AddItem(new SelectionPopupItem()
            {   
                ID = gear.PlayfabItemId,
                DisplayName = gear.PublicGearName,
                SubTitle = "Testing",
                Description = "Testing Description",
                clickedAction = OnItemClicked,
            });
        }
        
        _menu.AddHeader(header);
    }

    public void Show()
    {
        _menu.Show();
    }

    public void Hide()
    {
        _menu.Hide();
    }
    
    private void OnItemClicked(ISelectionPopupItem item)
    {
        if (!TryGetItemByIDFromAvailable(item.ID, out var gear))
        {
            return;
        }
        
        PlayerBackpackManager.ResetLocalAmmoStorage(full: true);
        PlayerBackpackManager.EquipLocalGear(gear);
        GearManager.RegisterGearInSlotAsEquipped(gear.PlayfabItemInstanceId, _slot);

        item.CloseMenu = true;
        
        FocusStateManager.ExitMenu();
    }

    public bool TryGetItemByIDFromAvailable(string id, out GearIDRange gear)
    {
        gear = _gear.FirstOrDefault(gear => gear.PlayfabItemId == id);
        return gear != null;
    }
    
    public bool TryGetItemFromBP(SNet_Player player, out BackpackItem item)
    {
        return PlayerBackpackManager.TryGetItem(player, _slot, out item);
    }
    
    public bool PlayerHasEquipped(SNet_Player player, GearIDRange gear, out BackpackItem backpackItem)
    {
        return TryGetItemFromBP(player, out backpackItem)
               && backpackItem.GearIDRange.IsEqual(gear);
    }

    public void Ree(GearIDRange gear, SpriteRenderer renderer)
    {
        uint checksum = gear.GetChecksum();
        GearManager.RegisterAsGearIconTargetCallback(checksum, eGearIconType.LoadoutBar, "playerSlot", (Action<RenderTexture>) delegate(RenderTexture iconTexture)
        {
            GearIconRendering.SetSpriteTexture(gear.GetChecksum(), renderer, iconTexture);
        });
    }
}