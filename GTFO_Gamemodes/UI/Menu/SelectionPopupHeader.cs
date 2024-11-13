using System;
using System.Collections.Generic;
using CellMenu;
using Gamemodes.Net;
using SNetwork;
using UnityEngine;

namespace Gamemodes.UI.Menu;

public class SelectionPopupHeader
{
    public string HeaderText { get; set; } = "No Text Set";
    public string IsActiveText { get; set; } = "<color=orange>Selected</color>";
    public string SetActiveConfirmText { get; set; } = "<color=orange>Click again to select.</color>";
    public Func<SelectionPopupHeader, ISelectionPopupItem, bool> AllowedToSelectItem { get; set; }
    public List<ISelectionPopupItem> Items { get; } = new();
    
    public string ActiveItemID { get; set; }
    public string LastSelectedItemID { get; set; }
    
    public Action<SelectionPopupHeader> PreDraw { get; set; }
    
    private Action<int> _drawAction = null!;
    internal Action<int> DrawAction => _drawAction ??= Draw;
    internal SelectionPopupMenu Parent { get; set; }
    public bool IsUpdating { get; private set; }
    public ISelectionPopupItem LastClickedItem { get; private set; }
    
    private CM_PlayerLobbyBar _lobbyBar;

    public SelectionPopupHeader(string headerText)
    {
        HeaderText = headerText;
    }

    public void Show()
    {
        Parent.Show(Parent.GetKey(this));
    }

    public void Hide()
    {
        Parent.Hide();
    }
    
    public SelectionPopupHeader AddItem(ISelectionPopupItem item)
    {
        if (Items.Contains(item))
            return this;
        
        Items.Add(item);
        return this;
    }

    internal void Setup(CM_PlayerLobbyBar lobbyBar)
    {
        _lobbyBar = lobbyBar;
    }
    
    internal void Draw(int key)
    {
        if (_lobbyBar == null)
            return;
        IsUpdating = false;
        UpdateContent(key);
    }
    
    private void UpdateContent(int key, float? scrollPosOffset = null, float? scrollHandlePosY = null)
    {
        PreDraw?.Invoke(this);
        
        var list = new Il2CppSystem.Collections.Generic.List<iScrollWindowContent>();
        
        int c = 0;
        foreach(var item in Items)
        {
            var scrollItem = GOUtil.SpawnChildAndGetComp<CM_LobbyScrollItem>(_lobbyBar.m_clothesCardPrefab, _lobbyBar.transform);
            list.Add(new iScrollWindowContent(scrollItem.Pointer));

            scrollItem.TextMeshRoot = _lobbyBar.m_parentPage.transform;
            scrollItem.SetupFromLobby(_lobbyBar.transform, _lobbyBar, true);
            scrollItem.ForcePopupLayer();
            scrollItem.transform.localScale = Vector3.one;

            scrollItem.m_nameText.SetText($"{item.DisplayName}");

            var isSelected = item.ID == LastSelectedItemID;

            item.IsSelected = isSelected;
            
            scrollItem.IsSelected = isSelected;

            var isActive = item.ID == ActiveItemID;
            
            item.IsActive = isActive;
            
            var emptyIcon = scrollItem.transform.FindChild("EmptyIcon"); // '+' icon
            emptyIcon.gameObject.SetActive(false);

            var bg = scrollItem.transform.FindChild("Background");
            bg.gameObject.SetActive(isSelected);

            string overtitleText = string.Empty;

            item.IsAllowedToSelect = AllowedToSelectItem?.Invoke(this, item) ?? true;
            
            if (isSelected)
            {
                // Setting sprite to null so it correctly refreshes the size/aspect ratio on the new one
                _lobbyBar.m_popupScrollWindow.InfoBox.SetInfoBox(item.DisplayName, item.SubTitle, item.Description, string.Empty, string.Empty, null);
                _lobbyBar.m_popupScrollWindow.InfoBox.SetInfoBox(item.DisplayName, item.SubTitle, item.Description, string.Empty, string.Empty, item.SpriteLarge);

                if (item.IsAllowedToSelect)
                    overtitleText = SetActiveConfirmText;
            }

            if (isActive)
            {
                overtitleText = IsActiveText;
            }
            
            var subtitleText = item.ID;

            item.SubtitleTextCustomization(ref overtitleText, ref subtitleText);

            scrollItem.m_subTitleText.SetText(overtitleText);

            scrollItem.m_descText.SetText($"   <#666>{subtitleText}</color>");

            scrollItem.OnBtnPressCallback = new Action<int>(_ =>
            {
                var doubleClick = LastSelectedItemID == item.ID;
                
                item.WasDoubleClick = doubleClick;
                
                item.OnClicked();

                if (item.CloseMenu)
                {
                    item.CloseMenu = false;
                    Hide();
                    return;
                }
                
                if (doubleClick)
                    ActiveItemID = item.ID;
                
                LastSelectedItemID = item.ID;

                _lobbyBar.m_popupScrollWindow.InfoBox.SetInfoBox(item.DisplayName, item.SubTitle, item.Description, string.Empty, string.Empty, item.SpriteLarge);
                
                IsUpdating = true;
                LastClickedItem = item;
                UpdateContent(key, _lobbyBar.m_popupScrollWindow.m_posOffset, _lobbyBar.m_popupScrollWindow.m_scrollBar.GetHandlePosition().y);
            });

            if (item.SpriteSmall != null)
            {
                scrollItem.m_icon.sprite = item.SpriteSmall;
            }

            scrollItem.PlayIntro(c * 0.1f, -1);

            scrollItem.m_alphaSpriteOnHover = true;
            scrollItem.m_alphaTextOnHover = true;
            
            c++;
        }

        _lobbyBar.m_popupScrollWindow.SetContentItems(list, 0f);

        foreach(var item in list)
        {
            var scrollItem = item.TryCast<CM_LobbyScrollItem>();
            if (scrollItem == null)
                continue;
            scrollItem.UpdateSizesAndOffsets();
        }
 
        if (scrollPosOffset.HasValue)
            ScrollTo(scrollPosOffset.Value, scrollHandlePosY ?? 0);
        
        _lobbyBar.Select();
        _lobbyBar.ShowPopup();
        _lobbyBar.m_popupScrollWindow.SelectHeader(key);
    }

    private void ScrollTo(float posOffset, float handleY)
    {
        var window = _lobbyBar.m_popupScrollWindow;
        
        if (!window.m_scrollBar.Visible)
        {
            return;
        }
        window.m_posOffset = Mathf.Clamp(posOffset, 0f, window.m_contentContainerHeight - window.m_windowHeight);
        Vector2 position = window.m_contentContainer.GetPosition();
        position.y = window.m_posStart + window.m_posOffset;
        window.m_contentContainer.SetPosition(position);
        window.UpdateContentItemVisibility();
        
        var pos = window.m_scrollBar.GetHandlePosition();
        window.m_scrollBar.SetHandlePosition(new Vector2(pos.x, handleY));
    }
}