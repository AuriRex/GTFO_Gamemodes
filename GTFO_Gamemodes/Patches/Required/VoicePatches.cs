using Gamemodes.Components;
using Gamemodes.Extensions;
using HarmonyLib;
using Player;
using SNetwork;
using static Gamemodes.PatchManager;
using PlayerVoiceManager = Gamemodes.Core.Voice.PlayerVoiceManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.Setup))]
public static class PlayerAgent__Setup__Patch
{
    public static readonly string PatchGroup = PatchGroups.PROXIMITY_VOICE;
    
    public static void Postfix(PlayerAgent __instance)
    {
        __instance.gameObject.GetOrAddComponent<ProximityVoice>();
    }
}

[HarmonyPatch(typeof(PUI_VoiceControls), nameof(PUI_VoiceControls.SetPlayer))]
public static class PUI_VoiceControls__SetPlayer__Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(PUI_VoiceControls __instance, SNet_Player player)
    {
        (float volume, bool muted) prefs = PlayerVoiceManager.PlayerPrefs.GetPreferences(player.Lookup);
        
        __instance.m_slider.Value = prefs.volume;
        __instance.m_muteToggleButton.Value = prefs.muted;
    }
}

[HarmonyPatch(typeof(PUI_VoiceControls), nameof(PUI_VoiceControls.OnVolumeChanged))]
public static class PUI_VoiceControls__OnVolumeChanged__Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static bool Prefix(PUI_VoiceControls __instance, float volume)
    {
        PlayerVoiceManager.PlayerPrefs.SetPreference(__instance.m_playerLookup, volume);
        return false;
    }
}

[HarmonyPatch(typeof(PUI_VoiceControls), nameof(PUI_VoiceControls.OnMuteChanged))]
public static class PUI_VoiceControls__OnMuteChanged__Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static bool Prefix(PUI_VoiceControls __instance, bool muted)
    {
        PlayerVoiceManager.PlayerPrefs.SetPreference(__instance.m_playerLookup, volume: null, muted);
        PlayerVoiceManager.SetMuted(__instance.m_playerLookup, muted);
        return false;
    }
}

[HarmonyPatch(typeof(PUI_VoiceControls), nameof(PUI_VoiceControls.Update))]
public static class PUI_VoiceControls__Update__Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static bool Prefix()
    {
        return false;
    }
}