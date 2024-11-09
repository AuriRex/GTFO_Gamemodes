using System;
using UnityEngine;

namespace Gamemodes.UI.Menu;

public class SelectionPopupItem : ISelectionPopupItem
{
    public string DisplayName { get; set; }
    public string ID { get; set; }
    public string SubTitle { get; set; }
    public string Description { get; set; }
    public Sprite SpriteSmall { get; set; }
    public Sprite SpriteLarge { get; set; }

    public bool WasDoubleClick { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
    public bool IsAllowedToSelect { get; set; }
    public bool CloseMenu { get; set; }
    
    public Action<ISelectionPopupItem> clickedAction = null!;
    public Func<ISelectionPopupItem, string, string> upperTextCustomization = null!;
    public Func<ISelectionPopupItem, string, string> subtitleTextCustomization = null!;
    
    public void OnClicked()
    {
        clickedAction?.Invoke(this);
    }

    public void SubtitleTextCustomization(ref string suptitleText, ref string subtitleText)
    {
        var up = upperTextCustomization?.Invoke(this, suptitleText);
        if (up != null)
            suptitleText = up;
        
        var sub = subtitleTextCustomization?.Invoke(this, subtitleText);
        if (sub != null)
            subtitleText = sub;
    }
}