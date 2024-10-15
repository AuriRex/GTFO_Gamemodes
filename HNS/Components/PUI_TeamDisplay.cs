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

    private GameObject _titleGameObject;
    private TextMeshPro _titleTMP;

    private PUI_GameObjectives _gameObjectives;

    private const float UPDATE_INTERVAL = 5;
    private float _updateTimer;

    private bool _isSetup = false;

    public void Awake()
    {
        if (_isSetup)
            return;

        _gameObjectives = GetComponent<PUI_GameObjectives>();

        _gameObjectives.m_bodyInfoHolder.transform.localPosition = new Vector3(-600, -100, 0); // Moves key items etc out to the left of the screen

        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;
        NetworkingManager.OnPlayerCountChanged += OnPlayerCountChanged;

        UpdateTitle(string.Empty);

        _isSetup = true;

        if (PlayerEntry.HasPrefab)
        {
            return;
        }

        var prefab = Instantiate(_gameObjectives.m_headerHolder, transform);

        prefab.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

        prefab.SetActive(false);

        PlayerEntry.Parent = transform;
        PlayerEntry.Prefab = prefab;
    }

    public void UpdateTitle(string title)
    {
        if (_titleGameObject == null)
        {
            _titleGameObject = Instantiate(_gameObjectives.m_headerHolder, transform);

            _titleTMP = _titleGameObject.GetComponentInChildren<TextMeshPro>();

            var background = _titleGameObject.GetComponentInChildren<SpriteRenderer>();

            background.color = PlayerEntry.COLOR_MISC;

            _titleGameObject.transform.localPosition = new Vector3(1.4f, -50f - PlayerEntry.SPACING * 0.5f, 0);
            _titleGameObject.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            _titleGameObject.SetActive(true);
        }

        _titleTMP.SetText(title);
    }

    public void OnDestroy()
    {
        NetworkingManager.OnPlayerChangedTeams -= OnPlayerChangedTeams;
        NetworkingManager.OnPlayerCountChanged -= OnPlayerCountChanged;

        foreach(var entry in _entries)
        {
            entry.Dispose();
        }

        Destroy(_titleGameObject);
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
        foreach (var player in NetworkingManager.AllValidPlayers.OrderByDescending(w => $"{w.Team}_{w.NickName}"))
        {
            if (!TryGetPlayerEntry(player, out var entry))
            {
                entry = CreatePlayerEntry(player);
            }

            entry?.Update();
        }
    }

    public void RecreateTeamDisplay()
    {
        foreach (var entry in _entries)
        {
            entry.Dispose();
        }
        _entries.Clear();

        UpdateTeamDisplay();
    }

    private void OnPlayerCountChanged()
    {
        RecreateTeamDisplay();
    }

    private void OnPlayerChangedTeams(PlayerWrapper player, int team)
    {
        RecreateTeamDisplay();
    }

    private PlayerEntry CreatePlayerEntry(PlayerWrapper player)
    {
        if (!player.HasAgent)
            return null;

        var entry = PlayerEntry.Create(player, _entries.Count + 2);
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
        public static bool HasPrefab => Prefab != null;
        internal static Transform Parent { private get; set; }
        internal static GameObject Prefab { private get; set; }

        private readonly GameObject _gameObject;
        private readonly PlayerWrapper _player;

        public const float SPACING = 32f;
        public const float OPACITY = 0.125f;

        public static readonly Color COLOR_MISC = new Color(Color.gray.r, Color.gray.g, Color.gray.b, OPACITY);
        public static readonly Color COLOR_HIDER = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, OPACITY);
        public static readonly Color COLOR_SEEKER = new Color(Color.red.r, Color.red.g, Color.red.b, OPACITY);

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
            Color col = COLOR_MISC;
            string extraText = string.Empty;
            switch((GMTeam)_player.Team)
            {
                default:
                case GMTeam.PreGameAndOrSpectator:
                    break;
                case GMTeam.Seekers:
                    team = "S";
                    col = COLOR_SEEKER;

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
                    col = COLOR_HIDER;
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
