using System;
using System.Collections.Generic;
using Gamemodes.Net;
using TMPro;
using UnityEngine;

namespace Gamemodes.Components;

public partial class PUI_TeamDisplay
{
    private sealed class PlayerEntry : IDisposable
    {
        public static bool HasPrefab => Prefab != null;
        internal static Transform Parent { private get; set; }
        internal static GameObject Prefab { private get; set; }

        private readonly GameObject _gameObject;
        private readonly PlayerWrapper _player;
        private readonly PUI_TeamDisplay _teamDisplay;
        
        public const float SPACING = 32f;

        public TextMeshPro Text { get; init; }
        public TextMeshPro ExtraText { get; init; }
        public SpriteRenderer Background { get; init; }
        public PlayerWrapper Wrapper => _player;

        private PlayerEntry(PlayerWrapper player, int index, PUI_TeamDisplay teamDisplay)
        {
            _gameObject = Instantiate(Prefab, Parent);
            _player = player;
            _teamDisplay = teamDisplay;
            Text = _gameObject.GetComponentInChildren<TextMeshPro>();

            var mainTextGo = Text.gameObject;

            var extraTextGo = Instantiate(mainTextGo, mainTextGo.transform.parent);

            ExtraText = extraTextGo.GetComponent<TextMeshPro>();
            ExtraText.SetText(string.Empty);

            Background = _gameObject.GetComponentInChildren<SpriteRenderer>();
            Update();
            _gameObject.transform.localPosition = new Vector3(1.4f, -50f - SPACING * index, 0);
            _gameObject.SetActive(true);
        }

        public static PlayerEntry Create(PlayerWrapper player, int index, PUI_TeamDisplay teamDisplay)
        {
            return new PlayerEntry(player, index, teamDisplay);
        }

        public void Update()
        {
            
            string extraText = string.Empty;
            
            var teamDisplay = _teamDisplay.TeamDisplay.GetValueOrDefault(_player.Team, TDD_DEFAULT) ?? TDD_DEFAULT;

            var team = teamDisplay.Identifier;
            var col = teamDisplay.Color;

            if (teamDisplay.UpdateExtraInfo != null)
            {
                extraText = teamDisplay.UpdateExtraInfo.Invoke(_player);
            }

            Text.SetText($"[{team}]  {_player.PlayerColorTag}{_player.NickName}</color>");

            if (!string.IsNullOrWhiteSpace(extraText))
            {
                extraText = $"<align=right>{extraText}</align>";
            }

            ExtraText.SetText(extraText);

            Background.color = col;
        }

        public void Dispose()
        {
            if (_gameObject == null)
                return;

            Destroy(_gameObject);
        }
    }
}