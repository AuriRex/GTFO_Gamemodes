using CellMenu;
using Gamemodes.Mode;
using Gamemodes.Net;
using SNetwork;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Gamemodes.UI;

internal static class LobbyUI
{
    private static string _selectedGamemodeID = null;
    private static CM_Item _changeGameModeButton;
    private static TextMeshPro _subText;

    internal static void Setup(CM_PageLoadout instance)
    {
        Plugin.L.LogDebug("Setting up Gamemodes UI ...");

        var timedButton = instance.m_guiLayer.AddRectComp(instance.m_readyButtonPrefab, GuiAnchor.TopCenter, new Vector2(500f, 0f), instance.m_readyButtonAlign).Cast<CM_TimedButton>();
        var go = timedButton.gameObject;

        var texts = timedButton.GetTexts();
        timedButton.enabled = false;
        UnityEngine.Object.Destroy(timedButton);
        
        _changeGameModeButton = go.AddComponent<CM_Item>();
        _changeGameModeButton.m_texts = texts;
        _changeGameModeButton.TextMeshRoot = instance.transform;
        _changeGameModeButton.SetText("<#ccc><b>Switch Gamemode</b></color>");

        go.SetActive(true);
        
        var action = new Action<int>((_) => {
            Plugin.L.LogDebug("Attempting to show popup ...");
            // Shouldn't matter which lobby slot we're picking, riiiight? :D
            ShowGamemodeSelectionPopup(instance.m_playerLobbyBars[0], instance.m_popupAlign);
        });

        _changeGameModeButton.m_onBtnPress = new UnityEvent();
        
        _changeGameModeButton.OnBtnPressCallback = action;

        var transform = go.transform;

        transform.FindChild("ProgressFillBase").gameObject.SetActive(false);
        transform.FindChild("ProgressFill").gameObject.SetActive(false);
        transform.FindChild("Progress").gameObject.SetActive(false);
        transform.FindChild("Arrow").gameObject.SetActive(false);

        _subText = transform.FindChild("PressAndHold").GetComponent<TextMeshPro>();

        SetSubText(GamemodeManager.CurrentMode.DisplayName);

        GamemodeManager.OnGamemodeChanged += OnModeChange;
        GameEvents.OnGameStateChanged += OnGameStateChanged;
    }

    private static void OnGameStateChanged(eGameStateName state)
    {
        if (_changeGameModeButton == null)
            return;

        if (state == eGameStateName.Generating)
        {
            _changeGameModeButton.gameObject.SetActive(false);
        }

        if (state == eGameStateName.Lobby)
        {
            _changeGameModeButton.gameObject.SetActive(true);
        }
    }

    private static void SetSubText(string text)
    {
        _subText.SetText($"<color=orange>{text}</color>");
    }

    private static void OnModeChange(ModeInfo mode)
    {
        SetSubText(mode.DisplayName);
    }

    public static void ShowGamemodeSelectionPopup(CM_PlayerLobbyBar __this, Transform align)
    {
        _selectedGamemodeID = GamemodeManager.CurrentMode?.ID;

        __this.m_popupVisible = true;
        __this.m_popupHolder.transform.position = align.position + new Vector3(300f, 0f, 0f);
        __this.m_popupScrollWindow.m_infoBoxWidth = 600f;
        __this.m_popupHolder.gameObject.SetActive(true);
        __this.m_popupItemHolder.gameObject.SetActive(true);
        __this.m_popupScrollWindow.SetSize(new Vector2(1600f, 760f));
        __this.m_popupScrollWindow.ResetHeaders();
        __this.m_popupScrollWindow.AddHeader("Gamemode Selection", 1, new Action<int>((_) => {
                UpdateGamemodeWindowInfo(__this, align);
            })
        );

        __this.m_popupScrollWindow.SetPosition(new Vector2(0f, 350f));
        __this.m_popupScrollWindow.RespawnInfoBoxFromPrefab(__this.m_popupInfoBoxWeaponPrefab);
        UpdateGamemodeWindowInfo(__this, align);
    }

    private static void UpdateGamemodeWindowInfo(CM_PlayerLobbyBar __this, Transform align)
    {
        var list = new Il2CppSystem.Collections.Generic.List<iScrollWindowContent>();

        int c = 0;
        foreach(var modeInfo in GamemodeManager.LoadedModes)
        {
            var scrollItem = GOUtil.SpawnChildAndGetComp<CM_LobbyScrollItem>(__this.m_clothesCardPrefab, __this.transform);
            list.Add(new iScrollWindowContent(scrollItem.Pointer));

            scrollItem.TextMeshRoot = __this.m_parentPage.transform;
            scrollItem.SetupFromLobby(__this.transform, __this, true);
            scrollItem.ForcePopupLayer(true, null);
            scrollItem.transform.localScale = Vector3.one;

            scrollItem.m_nameText.SetText($"{modeInfo.DisplayName}");

            var isSelected = modeInfo.ID == _selectedGamemodeID;

            scrollItem.IsSelected = isSelected;

            var isActive = modeInfo.ID == GamemodeManager.CurrentMode?.ID;

            var isVanilla = modeInfo.ID == ModeGTFO.MODE_ID;
            
            var emptyIcon = scrollItem.transform.FindChild("EmptyIcon"); // '+' icon
            emptyIcon.gameObject.SetActive(false);

            var bg = scrollItem.transform.FindChild("Background");
            bg.gameObject.SetActive(isSelected);

            string subtitleText = string.Empty;

            if (isSelected)
            {
                __this.m_popupScrollWindow.InfoBox.SetInfoBox(modeInfo.DisplayName, modeInfo.SubTitle, modeInfo.Description, string.Empty, string.Empty, modeInfo.SpriteLarge);

                if (SNet.IsMaster)
                {
                    subtitleText = "<color=orange>Click again to switch to mode.</color>";
                }
            }

            if (isActive)
            {
                subtitleText = "<color=orange>Active</color> ";
            }

            if (isVanilla && (string.IsNullOrEmpty(subtitleText) || isActive) && GamemodeManager.ModeSwitchCount > 1)
            {
                subtitleText = $"{subtitleText}<#F00>{(isActive ? string.Empty : "<alpha=#33>")}<u>/!\\</u> Restart Recommended to play Vanilla again!</color>";
            }

            scrollItem.m_subTitleText.SetText(subtitleText);

            scrollItem.m_descText.SetText($"   <#666>{modeInfo.ID}</color>");

            scrollItem.OnBtnPressCallback = new Action<int>((_) => {
                if (SNet.IsMaster && _selectedGamemodeID == modeInfo.ID)
                {
                    NetworkingManager.SendSwitchModeAll(modeInfo.ID);
                }

                _selectedGamemodeID = modeInfo.ID;

                __this.m_popupScrollWindow.InfoBox.SetInfoBox(modeInfo.DisplayName, modeInfo.SubTitle, modeInfo.Description, string.Empty, string.Empty, modeInfo.SpriteLarge);

                UpdateGamemodeWindowInfo(__this, align);
            });

            if (modeInfo.SpriteSmall != null)
            {
                scrollItem.m_icon.sprite = modeInfo.SpriteSmall;
            }

            scrollItem.PlayIntro(c * 0.1f, -1);

            scrollItem.m_alphaSpriteOnHover = true;
            scrollItem.m_alphaTextOnHover = true;

            c++;
        }

        __this.m_popupScrollWindow.SetContentItems(list, 0f);

        foreach(var item in list)
        {
            item.TryCast<CM_LobbyScrollItem>().UpdateSizesAndOffsets();
        }

        __this.Select();
        __this.ShowPopup();
        __this.m_popupScrollWindow.SelectHeader(1);
    }
}
