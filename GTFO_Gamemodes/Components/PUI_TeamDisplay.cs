using Gamemodes.Net;
using System.Collections.Generic;
using System.Linq;
using Gamemodes.Extensions;
using Gamemodes.UI;
using Il2CppInterop.Runtime.Attributes;
using TMPro;
using UnityEngine;

namespace Gamemodes.Components;

public partial class PUI_TeamDisplay : MonoBehaviour
{
    private readonly List<PlayerEntry> _entries = new();

    private GameObject _titleGameObject;
    private TextMeshPro _titleTMP;

    private PUI_GameObjectives _gameObjectives;

    private const float UPDATE_INTERVAL = 5;
    private float _updateTimer;

    private bool _isSetup;

    [HideFromIl2Cpp]
    private Dictionary<int, TeamDisplayData> TeamDisplay { get; } = new();

    public static PUI_TeamDisplay InstantiateOrGetInstanceOnWardenObjectives()
    {
        var go = GuiManager.PlayerLayer.WardenObjectives.gameObject;
        return go.GetOrAddComponent<PUI_TeamDisplay>();
    }
    
    public static bool DestroyInstanceOnWardenObjectives()
    {
        var go = GuiManager.PlayerLayer.WardenObjectives.gameObject;
        var comp = go.GetComponent<PUI_TeamDisplay>();
        if (comp == null)
            return false;
        
        Destroy(comp);
        return true;
    }

    public void Awake()
    {
        if (_isSetup)
            return;

        _gameObjectives = GetComponent<PUI_GameObjectives>();

        // Moves key items etc out to the left of the screen
        _gameObjectives.m_bodyInfoHolder.transform.localPosition = new Vector3(-600, -100, 0);

        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;
        NetworkingManager.OnPlayerCountChanged += OnPlayerCountChanged;

        UpdateTitle(string.Empty);

        _isSetup = true;

        if (PlayerEntry.HasPrefab)
            return;

        var prefab = Instantiate(_gameObjectives.m_headerHolder, transform);

        prefab.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

        prefab.SetActive(false);

        PlayerEntry.Parent = transform;
        PlayerEntry.Prefab = prefab;
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

    public void UpdateTitle(string title)
    {
        if (_titleGameObject == null)
        {
            _titleGameObject = Instantiate(_gameObjectives.m_headerHolder, transform);

            _titleTMP = _titleGameObject.GetComponentInChildren<TextMeshPro>();

            var background = _titleGameObject.GetComponentInChildren<SpriteRenderer>();

            background.color = COLOR_MISC;

            _titleGameObject.transform.localPosition = new Vector3(1.4f, -50f - PlayerEntry.SPACING * 0.5f, 0);
            _titleGameObject.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            _titleGameObject.SetActive(true);
        }

        _titleTMP.SetText(title);
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

    [HideFromIl2Cpp]
    private void OnPlayerChangedTeams(PlayerWrapper player, int team)
    {
        RecreateTeamDisplay();
    }

    [HideFromIl2Cpp]
    private PlayerEntry CreatePlayerEntry(PlayerWrapper player)
    {
        if (!player.HasAgent)
            return null;

        var entry = PlayerEntry.Create(player, _entries.Count + 2, this);
        _entries.Add(entry);
        return entry;
    }

    [HideFromIl2Cpp]
    private bool TryGetPlayerEntry(PlayerWrapper player, out PlayerEntry entry)
    {
        entry = _entries.FirstOrDefault(entry => entry.Wrapper == player);
        return entry != null;
    }
    
    [HideFromIl2Cpp]
    public PUI_TeamDisplay SetTeamDisplayData(int team, TeamDisplayData data)
    {
        TeamDisplay[team] = data;
        return this;
    }

    public const float COLOR_OPACITY = 0.125f;

    public static readonly Color COLOR_MISC = new(Color.gray.r, Color.gray.g, Color.gray.b, COLOR_OPACITY);
    
    public static readonly Color COLOR_YELLOW = new(Color.yellow.r, Color.yellow.g, Color.yellow.b, COLOR_OPACITY);
    public static readonly Color COLOR_MAGENTA = new(Color.magenta.r, Color.magenta.g, Color.magenta.b, COLOR_OPACITY);
    public static readonly Color COLOR_CYAN = new(Color.cyan.r, Color.cyan.g, Color.cyan.b, COLOR_OPACITY);
    
    public static readonly Color COLOR_RED = new(Color.red.r, Color.red.g, Color.red.b, COLOR_OPACITY);
    public static readonly Color COLOR_GREEN = new(Color.green.r, Color.green.g, Color.green.b, COLOR_OPACITY);
    public static readonly Color COLOR_BLUE = new(Color.blue.r, Color.blue.g, Color.blue.b, COLOR_OPACITY);
    
    public static readonly TeamDisplayData TDD_DEFAULT = new('/', COLOR_MISC);
}
