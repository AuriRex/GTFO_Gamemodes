using Gamemodes.Net;
using HNS.Core;
using HNS.Net.Packets;
using Player;
using SNetwork;
using System;
using System.Linq;

namespace HNS.Net;

internal static class NetSessionManager
{
    public static bool HasSession => CurrentSession != null && CurrentSession.IsActive;
    public static Session CurrentSession { get; private set; }

    internal static void Init()
    {
        NetworkingManager.RegisterEvent<pHNSGameStart>(OnGameStartReceived);
    }

    public static void SendStartGamePacket(params ulong[] seekers)
    {
        if (!SNet.IsMaster)
            return;

        var seekersA = new ulong[16];

        for(int i = 0; i < seekers.Length; i++)
        {
            if (i >= 16)
                break;

            seekersA[i] = seekers[i];
        }

        var data = new pHNSGameStart
        {
            SeekerCount = (byte)seekers.Length,
            Seekers = seekersA,
            SetupTimeSeconds = 60,
        };

        NetworkingManager.SendEvent(data, invokeLocal: true);
    }

    private static void OnGameStartReceived(ulong sender, pHNSGameStart data)
    {
        Plugin.L.LogWarning($"A new game is starting!");

        if (HasSession)
        {
            CurrentSession.EndSession();
        }

        CurrentSession = new();

        var isLocalPlayerSeeker = data.Seekers.Contains(NetworkingManager.LocalPlayerId);

        HideAndSeekMode.GameManager.StartGame(isLocalPlayerSeeker, data.SetupTimeSeconds);

        //PlayerManager.GetLocalPlayerAgent()
        // Start local countdown timer
        // blind seekers for timer duration
        // switch timer to countup after setup
    }
}
