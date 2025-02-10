using Player;

namespace Gamemodes.Core.Voice.Modulators;

public class PlayerDeadModulator : IVoiceVolumeModulator
{
    public PlayerVoiceManager.ApplyVoiceStates ApplyToStates => PlayerVoiceManager.ApplyVoiceStates.Downed;
    
    public void Modify(PlayerAgent player, ref float volume)
    {
        volume = 0f;
    }
}