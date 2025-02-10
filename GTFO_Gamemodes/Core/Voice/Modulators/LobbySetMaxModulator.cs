using Player;

namespace Gamemodes.Core.Voice.Modulators;

public class LobbySetMaxModulator : IVoiceVolumeModulator
{
    public PlayerVoiceManager.ApplyVoiceStates ApplyToStates => PlayerVoiceManager.ApplyVoiceStates.Lobby;
    public void Modify(PlayerAgent player, ref float volume)
    {
        volume = 1f;
    }
}