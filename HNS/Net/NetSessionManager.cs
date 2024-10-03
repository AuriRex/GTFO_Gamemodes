using Gamemodes.Net;
using HNS.Core;
using HNS.Net.Packets;
using Player;
using SNetwork;
using System;
using System.Linq;
using UnityEngine;

namespace HNS.Net;

internal static class NetSessionManager
{
    public static bool HasSession => CurrentSession != null && CurrentSession.IsActive;
    public static Session CurrentSession { get; private set; }

    internal static void Init()
    {
        NetworkingManager.RegisterEvent<pHNSGameStart>(OnGameStartReceived);
        NetworkingManager.RegisterEvent<pHNSGameStop>(OnGameStopReceived);
    }

    public static void SendStartGamePacket(params ulong[] seekers)
    {
        if (!SNet.IsMaster)
            return;

        if (!NetworkingManager.InLevel)
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

        CurrentSession = new(data.SetupTimeSeconds);

        var isLocalPlayerSeeker = data.Seekers.Contains(NetworkingManager.LocalPlayerId);

        HideAndSeekMode.GameManager.StartGame(isLocalPlayerSeeker, data.SetupTimeSeconds);

        Gamemodes.Plugin.PostLocalMessage("<#0C0>Hide and Seek round has started!");
        Gamemodes.Plugin.PostLocalMessage($"<#CCC>{data.SeekerCount} Seekers:");

        foreach(var id in data.Seekers)
        {
            if (id == 0)
                continue;

            NetworkingManager.GetPlayerInfo(id, out var info);

            Gamemodes.Plugin.PostLocalMessage($" - {info.PlayerColorTag}{info.NickName}");
        }

        //PlayerManager.GetLocalPlayerAgent()
        // Start local countdown timer
        // blind seekers for timer duration
        // switch timer to countup after setup
    }

    internal static void SendStopGamePacket()
    {
        if (!HasSession)
            return;

        if (!SNet.IsMaster)
            return;

        CurrentSession.EndSession();

        var data = new pHNSGameStop
        {
            Time = CurrentSession.EndTime.ToUnixTimeSeconds()
        };

        NetworkingManager.SendEvent(data, invokeLocal: true);
    }


    private static void OnGameStopReceived(ulong sender, pHNSGameStop data)
    {
        CurrentSession.EndSession(DateTimeOffset.FromUnixTimeSeconds(data.Time));

        HideAndSeekMode.GameManager.StopGame(CurrentSession);

        Plugin.L.LogDebug($"Session has ended. {CurrentSession.FinalTime}");
    }
}
