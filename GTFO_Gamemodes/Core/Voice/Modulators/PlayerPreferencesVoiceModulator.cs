using System.Collections.Generic;
using Player;

namespace Gamemodes.Core.Voice.Modulators;

internal class PlayerPreferencesVoiceModulator : IVoiceVolumeModulator
{
    private readonly Dictionary<ulong, (float volume, bool muted)> _voicePreference = new();

    public void SetPreference(ulong player, float? volume, bool? muted = null)
    {
        var preferences = GetPreferences(player);
        
        if (volume.HasValue)
            preferences.volume = volume.Value;
        
        if (muted.HasValue)
            preferences.muted = muted.Value;
        
        _voicePreference[player] = preferences;
    }
    
    public (float volume, bool muted) GetPreferences(ulong playerLookup)
    {
        if (!_voicePreference.TryGetValue(playerLookup, out var mod))
            return (1f, false);

        return mod;
    }
    
    public PlayerVoiceManager.ApplyVoiceStates ApplyToStates => PlayerVoiceManager.ApplyVoiceStates.InLevel |
                                                                PlayerVoiceManager.ApplyVoiceStates.Lobby |
                                                                PlayerVoiceManager.ApplyVoiceStates.Downed;
    public void Modify(PlayerAgent player, ref float volume)
    {
        if (!_voicePreference.TryGetValue(player.Owner.Lookup, out var mod))
            return;

        if (mod.muted)
        {
            volume = 0f;
            return;
        }
        
        volume *= mod.volume;
    }
}