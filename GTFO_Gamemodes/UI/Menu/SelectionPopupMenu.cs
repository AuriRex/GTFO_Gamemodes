using System.Collections.Generic;
using System.Linq;
using CellMenu;
using UnityEngine;

namespace Gamemodes.UI.Menu;

public class SelectionPopupMenu
{
    private readonly List<SelectionPopupHeader> _headers = new();

    private CM_PlayerLobbyBar _lobbyBar;

    public SelectionPopupMenu AddHeader(SelectionPopupHeader header)
    {
        if (_headers.Contains(header))
            return this;
        
        _headers.Add(header);
        header.Parent = this;
        return this;
    }
    
    public void Show(int key = 0)
    {
        _lobbyBar = CM_PageLoadout.Current.m_playerLobbyBars.FirstOrDefault(plb => plb.isActiveAndEnabled);

        if (_lobbyBar == null)
            _lobbyBar = CM_PageLoadout.Current.m_playerLobbyBars[0];
        
        var popupAlign = CM_PageLoadout.Current.m_popupAlign;
        
        ShowSelectionPopup(_lobbyBar, popupAlign, key);
    }

    public void Hide()
    {
        if (_lobbyBar == null)
            return;
        
        _lobbyBar.m_popupVisible = false;
        _lobbyBar.m_popupHolder.gameObject.SetActive(false);
        _lobbyBar.m_popupItemHolder.gameObject.SetActive(false);
    }

    public int GetKey(SelectionPopupHeader header)
    {
        return _headers.IndexOf(header);
    }

    public bool TryGetHeader(int key, out SelectionPopupHeader header)
    {
        header = null;
        if (key < 0 || key >= _headers.Count)
            return false;

        header = _headers[key];
        return header != null;
    }
    
    private void ShowSelectionPopup(CM_PlayerLobbyBar lobbyBar, Transform align, int keyToShow)
    {
        lobbyBar.m_popupVisible = true;
        lobbyBar.m_popupHolder.transform.position = align.position + new Vector3(300f, 0f, 0f);
        lobbyBar.m_popupScrollWindow.m_infoBoxWidth = 600f;
        lobbyBar.m_popupHolder.gameObject.SetActive(true);
        lobbyBar.m_popupItemHolder.gameObject.SetActive(true);
        lobbyBar.m_popupScrollWindow.SetSize(new Vector2(1600f, 760f));
        lobbyBar.m_popupScrollWindow.ResetHeaders();

        int key = 0;
        foreach (var header in _headers)
        {
            header.Setup(lobbyBar);
            lobbyBar.m_popupScrollWindow.AddHeader(header.HeaderText, key, header.DrawAction);
            key++;
        }

        lobbyBar.m_popupScrollWindow.SetPosition(new Vector2(0f, 350f));
        lobbyBar.m_popupScrollWindow.RespawnInfoBoxFromPrefab(lobbyBar.m_popupInfoBoxWeaponPrefab);

        if (TryGetHeader(keyToShow, out SelectionPopupHeader headerToShow))
        {
            headerToShow.Draw(keyToShow);
        }
    }
}