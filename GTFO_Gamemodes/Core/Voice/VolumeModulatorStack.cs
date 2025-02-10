using System;
using Gamemodes.Components;

namespace Gamemodes.Core.Voice;

/// <summary>
/// Modifies the volume of remote players whenever set through <see cref="PlayerVoiceManager.SetVolume"/><br/>
/// Doesn't work on its own, requires something to actually update the volume constantly (=> <see cref="ProximityVoice"/>)
/// </summary>
public class VolumeModulatorStack
{
    public IVoiceVolumeModulator[] Modulators { get; protected set; }
    
    public VolumeModulatorStack(params IVoiceVolumeModulator[] modulators)
    {
        Modulators = modulators;

        Modulators ??= Array.Empty<IVoiceVolumeModulator>();
    }
}