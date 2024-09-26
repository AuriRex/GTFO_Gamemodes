using Gamemodes.Net;
using HNS.Net.Packets;
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

    public static void SendStartGamePacket(ulong[] seekers)
    {
        if (!SNet.IsMaster)
            return;

        var data = new pHNSGameStart
        {
            SeekerCount = (byte)seekers.Length,
            Seekers = seekers,
            SetupTimeSeconds = 60,
        };

        NetworkingManager.SendEvent(data, invokeLocal: true);
    }

    private static void OnGameStartReceived(ulong sender, pHNSGameStart data)
    {
        Plugin.L.LogWarning($"Game is starting!");

        if (HasSession)
        {
            CurrentSession.EndSession();
        }

        CurrentSession = new();

        var isLocalPlayerSeeker = data.Seekers.Contains(NetworkingManager.LocalPlayerId);

        // Start local countdown timer
        // blind seekers for timer duration
        // switch timer to countup after setup
    }
}
