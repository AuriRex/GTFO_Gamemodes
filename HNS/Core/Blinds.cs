using Gamemodes.Net;
using LevelGeneration;
using Player;
using System;
using UnityEngine;

namespace HNS.Core;
internal class Blinds : IDisposable
{
    private readonly LocalPlayerAgent _localPlayer;
    private readonly Vector3 _pos;
    private readonly Vector3 _rot;
    private readonly eDimensionIndex _dim;

    public Blinds(LocalPlayerAgent localPlayer, uint dimensionDataId = 14)
    {
        _localPlayer = localPlayer;
        _pos = _localPlayer.Position;
        _rot = _localPlayer.TargetLookDir;
        _dim = _localPlayer.DimensionIndex;

        Builder.CurrentFloor.GetArenaDimension(dimensionDataId, (uint) _localPlayer.PlayerSlotIndex, out var arena);

        var dim = arena.DimensionIndex;
        var pos = arena.GetStartCourseNode().GetRandomPositionInside();

        _localPlayer.TryWarpTo(dim, pos, _rot, false);
    }

    public void Dispose()
    {
        if (!NetworkingManager.InLevel)
            return;

        if (_localPlayer == null)
            return;

        _localPlayer.TryWarpTo(_dim, _pos, _rot, false);
    }
}
