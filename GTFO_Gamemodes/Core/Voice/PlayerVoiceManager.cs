using System;
using System.Collections.Generic;
using Gamemodes.Core.Voice.Modulators;
using Player;
using SNetwork;
using UnityEngine;

namespace Gamemodes.Core.Voice;

public static class PlayerVoiceManager
{
    private static VolumeModulatorStack _defaultModulatorStack;
    public static event Action<PlayerAgent, float> OnPlayerVoiceVolumeChanged;
    
    private static VolumeModulatorStack _modulatorStack;
    private static eGameStateName _currentState;
    internal static PlayerPreferencesVoiceModulator PlayerPrefs { get; } = new();
    
    internal static void Init()
    {
        GameEvents.OnGameStateChanged += OnGameStateChanged;

        _defaultModulatorStack = new VolumeModulatorStack(new LobbySetMaxModulator());
        _modulatorStack = _defaultModulatorStack;
    }

    public static void ResetModulatorStack()
    {
        _modulatorStack = _defaultModulatorStack;
    }

    public static void SetModulatorStack(VolumeModulatorStack stack)
    {
        if (stack == null)
            return;
        
        _modulatorStack = stack;
    }
    
    private static void OnGameStateChanged(eGameStateName state)
    {
        _currentState = state;
    }
    
    private static PlayerChatManager.RemotePlayerVoiceSettings GetSettings(SNet_Player player)
    {
        return player == null ? default : GetSettings(player.Lookup);
    }
    
    private static PlayerChatManager.RemotePlayerVoiceSettings GetSettings(ulong player)
    {
        if (player == 0L || PlayerChatManager.Current == null)
        {
            return default;
        }
        return PlayerChatManager.Current.GetRemotePlayerVoiceSettings(player);
    }

    private static void SetSettings(SNet_Player player, PlayerChatManager.RemotePlayerVoiceSettings settings)
    {
        if (player == null)
        {
            return;
        }
        SetSettings(player.Lookup, settings);
    }
    
    private static void SetSettings(ulong player, PlayerChatManager.RemotePlayerVoiceSettings settings)
    {
        if (player == 0L || PlayerChatManager.Current == null)
        {
            return;
        }
        PlayerChatManager.Current.SetRemotePlayerVoiceSettings(player, settings);
    }

    [Flags]
    public enum ApplyVoiceStates
    {
        None = 0,
        All = 1,
        Lobby = 2,
        InLevel = 4,
        Downed = 8,
    }

    public static Dictionary<string, float> debugValues = new();
    
    public static void SetVolume(PlayerAgent player, float volume)
    {
        debugValues.Clear();
        
        debugValues["entry>>"] = volume;
        
        foreach (var mod in _modulatorStack.Modulators)
        {
            if (mod == null)
                continue;

            bool checkPassed = mod.ApplyToStates.HasFlag(ApplyVoiceStates.All);

            if (mod.ApplyToStates.HasFlag(ApplyVoiceStates.Downed) && player.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed)
                checkPassed = true;

            if (mod.ApplyToStates.HasFlag(ApplyVoiceStates.InLevel) && _currentState == eGameStateName.InLevel)
                checkPassed = true;

            if (mod.ApplyToStates.HasFlag(ApplyVoiceStates.Lobby) && _currentState == eGameStateName.Lobby)
                checkPassed = true;

            if (!checkPassed)
                continue;

            debugValues[mod.GetType().Name] = volume;
            
            mod.Modify(player, ref volume);
        }
        
        debugValues["pre-prefs>>"] = volume;
        
        // Player preferences last
        PlayerPrefs.Modify(player, ref volume);
        
        debugValues["final>>"] = volume;
        
        var settings = GetSettings(player.Owner);
        
        settings.LocalVolume = Mathf.Clamp01(volume);
        
        SetSettings(player.Owner, settings);

        OnPlayerVoiceVolumeChanged?.Invoke(player, volume);
    }

    public static void SetMuted(PlayerAgent player, bool muted)
    {
        SetMuted(player.Owner.Lookup, muted);
    }
    
    public static void SetMuted(ulong player, bool muted)
    {
        var settings = GetSettings(player);

        settings.LocallyMuted = muted;
        
        SetSettings(player, settings);
    }

    public static bool TryGetActiveModulator<T>(out T modulator)
    {
        foreach (var mod in _modulatorStack.Modulators)
        {
            if (mod == null)
                continue;

            if (mod.GetType() == typeof(T))
            {
                modulator = (T)mod;
                return true;
            }
        }
        
        modulator = default;
        return false;
    }
}