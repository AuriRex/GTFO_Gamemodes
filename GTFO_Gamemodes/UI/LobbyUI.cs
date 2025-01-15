using CellMenu;
using Gamemodes.Core;
using Gamemodes.Net;
using SNetwork;
using System;
using Gamemodes.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Gamemodes.UI;

internal static class LobbyUI
{
    private static CM_Item _changeGameModeButton;
    private static TextMeshPro _subText;

    internal static void Setup(CM_PageLoadout instance)
    {
        Plugin.L.LogDebug("Setting up Gamemodes UI ...");

        var timedButton = instance.m_guiLayer.AddRectComp(instance.m_readyButtonPrefab, GuiAnchor.TopCenter, new Vector2(500f, 100f), instance.m_readyButtonAlign).Cast<CM_TimedButton>();
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
            TrySetupOrGetMenu().Show();
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

    private static SelectionPopupMenu _menu;
    private static SelectionPopupMenu TrySetupOrGetMenu()
    {
        if (_menu != null)
            return _menu;

        var header = new SelectionPopupHeader("Gamemode Selection");
        
        header.ActiveItemID = ModeGTFO.MODE_ID;
        header.LastSelectedItemID = ModeGTFO.MODE_ID;
        header.IsActiveText = "<color=orange>Active</color>";
        header.SetActiveConfirmText = "<color=orange>Click again to switch to mode.</color>";

        header.AllowedToSelectItem = (self, item) =>
        {
            return SNet.IsMaster
                   && (!NetworkingManager.InLevel
                       || (NetworkingManager.InLevel && (GamemodeManager.CurrentSettings?.AllowMidGameModeSwitch ?? false)));
        };
        
        header.PreDraw = self =>
        {
            self.ActiveItemID = GamemodeManager.CurrentMode?.ID;
            if (!self.IsUpdating)
            {
                self.LastSelectedItemID = GamemodeManager.CurrentMode?.ID;
            }
        };
        
        foreach (var modeInfo in GamemodeManager.LoadedModes)
        {
            header.AddItem(new SelectionPopupItem()
            {
                DisplayName = modeInfo.DisplayName,
                SubTitle = modeInfo.SubTitle,
                ID = modeInfo.ID,
                Description = modeInfo.Description,
                SpriteSmall = modeInfo.SpriteSmall,
                SpriteLarge = modeInfo.SpriteLarge,
                clickedAction = ModeSelectItemClickedAction,
                upperTextCustomization = UpperTextCustomization
            });
        }
            
        
        _menu = new SelectionPopupMenu().AddHeader(header);

        return _menu;
    }

    private static void ModeSelectItemClickedAction(ISelectionPopupItem self)
    {
        if (SNet.IsMaster && self.WasDoubleClick && !self.IsActive)
        {
            NetworkingManager.SendSwitchModeAll(self.ID);
        }
    }

    private static string UpperTextCustomization(ISelectionPopupItem self, string upperText)
    {
        var isVanilla = self.ID == ModeGTFO.MODE_ID;
        if (isVanilla && (string.IsNullOrEmpty(upperText) || self.IsActive) && GamemodeManager.ModeSwitchCount > 1)
        {
            return $"{upperText}<#F00>{(self.IsActive ? string.Empty : "<alpha=#33>")}<u>/!\\</u> Restart Recommended to play Vanilla again!</color>";
        }

        return upperText;
    }
}
