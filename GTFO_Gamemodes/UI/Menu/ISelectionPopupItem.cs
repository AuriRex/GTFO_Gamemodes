using UnityEngine;

namespace Gamemodes.UI.Menu;

public interface ISelectionPopupItem
{
    public string DisplayName { get; }
    public string ID { get; }
    public string SubTitle { get; }
    public string Description { get; }
    public Sprite SpriteSmall { get; }
    public Sprite SpriteLarge { get; }

    public bool WasDoubleClick { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
    public bool IsAllowedToSelect { get; set; }
    public bool CloseMenu { get; set; }

    public void OnClicked();

    public void SubtitleTextCustomization(ref string suptitleText, ref string subtitleText)
    {
        
    }
}