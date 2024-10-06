using Gamemodes.Net;
using HNS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace HNS.Components;

internal class PUI_TeamDisplay : MonoBehaviour
{
    private readonly List<PlayerEntry> _entries = new();

    private PUI_GameObjectives _gameObjectives;

    private const float UPDATE_INTERVAL = 5;
    private float _updateTimer;

    public void Start()
    {
        _gameObjectives = GetComponent<PUI_GameObjectives>();

        _gameObjectives.m_bodyInfoHolder.transform.localPosition = new Vector3(-600, -100, 0); // Moves key items etc out to the left of the screen

        var prefab = Instantiate(_gameObjectives.m_headerHolder, transform);

        prefab.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

        prefab.SetActive(false);

        PlayerEntry.Parent = transform;
        PlayerEntry.Prefab = prefab;

        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;
        NetworkingManager.OnPlayerCountChanged += OnPlayerCountChanged;
    }

    public void OnDestroy()
    {
        NetworkingManager.OnPlayerChangedTeams -= OnPlayerChangedTeams;
        NetworkingManager.OnPlayerCountChanged -= OnPlayerCountChanged;
    }

    public void Update()
    {
        _updateTimer -= Time.deltaTime;

        if (_updateTimer <= 0)
        {
            _updateTimer = UPDATE_INTERVAL;
            UpdateTeamDisplay();
        }
    }

    public void UpdateTeamDisplay()
    {
        foreach(var player in NetworkingManager.AllValidPlayers.OrderByDescending(w => w.Team))
        {
            if (!TryGetPlayerEntry(player, out var entry))
            {
                entry = CreatePlayerEntry(player);
            }

            entry?.Update();
        }
    }

    private void OnPlayerCountChanged()
    {
        foreach (var entry in _entries)
        {
            entry.Dispose();
        }
        _entries.Clear();

        UpdateTeamDisplay();
    }

    private void OnPlayerChangedTeams(PlayerWrapper player, int team)
    {
        UpdateTeamDisplay();
    }

    private PlayerEntry CreatePlayerEntry(PlayerWrapper player)
    {
        if (!player.HasAgent)
            return null;

        var entry = PlayerEntry.Create(player, _entries.Count);
        _entries.Add(entry);
        return entry;
    }

    private bool TryGetPlayerEntry(PlayerWrapper player, out PlayerEntry entry)
    {
        entry = _entries.FirstOrDefault(entry => entry.Wrapper == player);
        return entry != null;
    }

    private class PlayerEntry : IDisposable
    {
        internal static Transform Parent { private get; set; }
        internal static GameObject Prefab { private get; set; }

        private readonly GameObject _gameObject;
        private readonly PlayerWrapper _player;

        private const float SPACING = 32f;
        private const float OPACITY = 0.125f;

        private static readonly Color _colorRest = new Color(Color.gray.r, Color.gray.g, Color.gray.b, OPACITY);
        private static readonly Color _colorHider = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, OPACITY);
        private static readonly Color _colorSeeker = new Color(Color.red.r, Color.red.g, Color.red.b, OPACITY);

        public TextMeshPro Text { get; init; }
        public TextMeshPro ExtraText { get; init; }
        public SpriteRenderer Background { get; init; }
        public PlayerWrapper Wrapper => _player;

        private PlayerEntry(PlayerWrapper player, int index)
        {
            _gameObject = Instantiate(Prefab, Parent);
            _player = player;
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

        public static PlayerEntry Create(PlayerWrapper player, int index)
        {
            return new PlayerEntry(player, index);
        }

        public void Update()
        {
            string team = "/";
            Color col = _colorRest;
            string extraText = string.Empty;
            switch((GMTeam)_player.Team)
            {
                default:
                case GMTeam.PreGameAndOrSpectator:
                    break;
                case GMTeam.Seekers:
                    team = "S";
                    col = _colorSeeker;

                    if (_player.CanBeSeenByLocalPlayer() && _player.HasAgent)
                    {
                        var area = _player.PlayerAgent.CourseNode?.m_area;

                        if (area == null)
                            break;

                        extraText = $"<color=white>[ZONE {area.m_zone.NavInfo.Number}, Area {area.m_navInfo.Suffix}]";
                    }

                    break;
                case GMTeam.Hiders:
                    team = "H";
                    col = _colorHider;
                    break;
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
