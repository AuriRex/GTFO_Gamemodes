using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Components;
using Gamemodes.Net;
using Gamemodes.UI;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using LevelGeneration;
using Player;
using SNetwork;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gamemodes.Mode.TestModes;
#if DEBUG
public class GtfoButTheLevelSplitsUp : GamemodeBase
{
    public override string ID => "RandomDelete";
    public override string DisplayName => "GTFO but the level splits up";

    public override string Description => "Oh no ... xd";

    public override ModeSettings Settings => new ModeSettings()
    {
        
    };

    private GameObject _gameobject;
    private static TimerHUD _timerHUD;
    private ChaosManager _chaos;

    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct pStart
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool Hi;
    }
    
    public override void Init()
    {
        NetworkingManager.RegisterEvent<pStart>(OnStartReceived);
        
        _gameobject = new GameObject($"{nameof(GtfoButTheLevelSplitsUp)}_Manager");
        
        ClassInjector.RegisterTypeInIl2Cpp<ChaosManager>();
        
        Object.DontDestroyOnLoad(_gameobject);
        _gameobject.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;

        _timerHUD = _gameobject.AddComponent<TimerHUD>();
        _chaos = _gameobject.AddComponent<ChaosManager>();
        
        _gameobject.SetActive(false);
        
        GameEvents.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnStartReceived(ulong sender, pStart start)
    {
        _chaos.StartMadness(Builder.SessionSeedRandom.Seed);
    }

    public override void Enable()
    {
        _gameobject.SetActive(true);
    }

    public override void Disable()
    {
        _gameobject.SetActive(false);
    }

    private void OnGameStateChanged(eGameStateName state)
    {
        if (state == eGameStateName.InLevel)
        {
            _timerHUD.StartCountdown(5, Style, "Time remaining until chaos: [COUNTDOWN]");
            _timerHUD.ResetGameTimer();
            _timerHUD.StartGameTimer();
            
            if (SNet.IsMaster)
                CoroutineManager.StartCoroutine(Coroutine().WrapToIl2Cpp());

            return;
        }

        if (_chaos.IsActive)
        {
            _chaos.StopMadness();
            _timerHUD.StopGameTimer();
        }
    }

    private IEnumerator Coroutine()
    {
        var yielder = new WaitForSeconds(5f);
        yield return yielder;
        
        NetworkingManager.SendEvent<pStart>(new(), invokeLocal: true);
    }

    private static TimerStyleOverride _overrideStyle = new TimerStyleOverride(true, TimerDisplayStyle.Red, true);
    private TimerStyleOverride Style()
    {
        return _overrideStyle;
    }

    private class ChaosManager : MonoBehaviour
    {
        private LG_Floor _floor;
        private LocalPlayerAgent _localPlayer;

        private int _instancesOfChaosCaused = 0;
        
        private float _earlySuperChaos;
        private System.Random _random;
        
        [HideFromIl2Cpp]
        public bool IsActive { get; private set; }
        
        [HideFromIl2Cpp]
        public void StartMadness(int seed, float chanceForEarlyChaos = 0.001f)
        {
            _instancesOfChaosCaused = 0;
            _random = new System.Random(seed);
            _earlySuperChaos = chanceForEarlyChaos;
            
            IsActive = true;
            enabled = true;
            
            _floor = LG_LevelBuilder.Current.m_currentFloor;
            _localPlayer = PlayerManager.GetLocalPlayerAgent().TryCast<LocalPlayerAgent>();
            
            StartCoroutine(ChaosEnsues(0.1f).WrapToIl2Cpp());
        }

        [HideFromIl2Cpp]
        private IEnumerator ChaosEnsues(float delay = 0.1f)
        {
            _floor.GetDimension(_localPlayer.DimensionIndex, out var dimension);

            var rootGo = dimension.DimensionLevel.gameObject;
            var trans = rootGo.transform.parent;

            PickRandomChild(trans, out var child, depth: 0);

            var path = GetGameObjectPathIndex(child);
            var pathNoIds = GetGameObjectPath(child);
            
            // DESTROY THE CHILD
            child.gameObject.SetActive(false);
            
            _instancesOfChaosCaused++;

            GtfoButTheLevelSplitsUp._timerHUD.GameTimerFormatText = $"Amount of Chaos caused: {_instancesOfChaosCaused}";
            
            var yielder = new WaitForSeconds(delay);
            yield return yielder;

            StartCoroutine(ChaosEnsues(delay).WrapToIl2Cpp());
        }

        private void PickRandomChild(Transform trans, out Transform child, int depth)
        {
            while (true)
            {
                if (trans.childCount == 0)
                {
                    child = trans;
                    return;
                }

                var rnd = _random.NextSingle();

                if (depth > 4 && rnd < _earlySuperChaos)
                {
                    child = trans;
                    return;
                }

                var c = 0;
                do
                {
                    var chosenOne = _random.Next(0, trans.childCount);
                    child = trans.GetChild(chosenOne);

                    c++;
                    if (c >= 100)
                        break;
                } while (!child.gameObject.activeSelf);
                
                
                trans = child;
                depth++;
            }
        }

        private string GetGameObjectPath(Transform transform)
        {
            Stack<string> path = new();
            while (true)
            {
                var parent = transform.parent;
                
                
                
                if (parent == null)
                    break;

                if (parent.GetComponent<LG_DimensionRoot>() != null)
                    break;
                
                path.Push(transform.name);
                transform = parent;
            }
            
            
            var sb = new StringBuilder();
            while (path.Count > 0)
            {
                sb.Append($"{path.Pop()}/");
            }
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
        
        private string GetGameObjectPathIndex(Transform transform)
        {
            Stack<int> path = new();
            while (true)
            {
                var parent = transform.parent;
                
                
                
                if (parent == null)
                    break;

                if (parent.GetComponent<LG_DimensionRoot>() != null)
                    break;
                
                path.Push(transform.GetSiblingIndex());
                transform = parent;
            }

            var sb = new StringBuilder();
            while (path.Count > 0)
            {
                sb.Append($"{path.Pop()}/");
            }
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        [HideFromIl2Cpp]
        public void StopMadness()
        {
            IsActive = false;
            enabled = false;
            
            StopAllCoroutines();
        }

        public void Update()
        {
            
        }
    }
}
#endif