﻿using Gamemodes.Net;
using HNS.Core;
using HNS.Net.Packets;
using SNetwork;
using System;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Core;
using HNS.Components;
using UnityEngine;

namespace HNS.Net;

internal static class NetSessionManager
{
    
#if DEBUG
    private const int SETUP_TIME_SECONDS = 5;
#else
    private const int SETUP_TIME_SECONDS = 60;
#endif
    
    public static bool HasSession => CurrentSession != null && CurrentSession.IsActive;
    public static Session CurrentSession { get; private set; }

    internal static void Init(NetEvents events)
    {
        events.RegisterEvent<pHNSGameStart>(OnGameStartReceived);
        events.RegisterEvent<pHNSGameStop>(OnGameStopReceived);
        events.RegisterEvent<pEpicTracer>(OnEpicTracerReceived);
        events.RegisterEvent<pMineAction>(OnMineActionReceived);
        events.RegisterEvent<pXRayAction>(OnXRayActionReceived);
    }

    public static void SendStartGamePacket(int? setupTime = null, params ulong[] seekers)
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
            SetupTimeSeconds = (byte) (setupTime ?? SETUP_TIME_SECONDS),
        };

        NetworkingManager.SendEvent(data, invokeLocal: true);
    }

    private static void OnGameStartReceived(ulong sender, pHNSGameStart data)
    {
        Plugin.L.LogWarning($"A new game is starting!");

        if (HasSession)
        {
            CurrentSession.EndSession();
            HideAndSeekMode.GameManager.StopGame(CurrentSession, aborted: true);
        }

        CurrentSession = new(data.SetupTimeSeconds);

        var isLocalPlayerSeeker = data.Seekers.Contains(NetworkingManager.LocalPlayerId);

        if (SNet.IsMaster)
        {
            foreach (var player in NetworkingManager.AllValidPlayers)
            {
                if (player.Team == (int)GMTeam.Camera)
                    continue;
                
                if (TeamHelper.IsSeeker(player.Team))
                    continue;

                if (data.Seekers.Contains(player.ID))
                {
                    var seekerTeam = TeamHelper.GetSeekerTeamForPlayer((GMTeam)player.Team);
                    if (!TeamHelper.IsSeeker(seekerTeam))
                        seekerTeam = GMTeam.Seekers;
                    NetworkingManager.AssignTeam(player.NetPlayer, (int)seekerTeam);
                    continue;
                }

                if (TeamHelper.IsHider(player.Team))
                    continue;

                var hiderTeam = TeamHelper.GetHiderTeamForPlayer((GMTeam)player.Team);
                if (!TeamHelper.IsHider(hiderTeam))
                    hiderTeam = GMTeam.Hiders;
                NetworkingManager.AssignTeam(player.NetPlayer, (int)hiderTeam);
            }
        }

        HideAndSeekMode.GameManager.StartGame(isLocalPlayerSeeker, data.SetupTimeSeconds, CurrentSession);

        Gamemodes.Plugin.PostLocalMessage("<#0C0>Hide and Seek round has started!");
        Gamemodes.Plugin.PostLocalMessage($"<#CCC>{data.SeekerCount} Seekers:");

        foreach(var id in data.Seekers)
        {
            if (id == 0)
                continue;

            NetworkingManager.GetPlayerInfo(id, out var info);

            Gamemodes.Plugin.PostLocalMessage($" - {info.PlayerColorTag}{info.NickName}");
        }
    }

    internal static void SendStopGamePacket(bool abortGame = false)
    {
        if (!HasSession)
            return;

        if (!SNet.IsMaster)
            return;

        CurrentSession.EndSession();

        var data = new pHNSGameStop
        {
            Time = CurrentSession.EndTime.ToUnixTimeSeconds(),
            Aborted = abortGame
        };

        NetworkingManager.SendEvent(data, invokeLocal: true);
    }


    private static void OnGameStopReceived(ulong sender, pHNSGameStop data)
    {
        if (CurrentSession == null)
            return;

        CurrentSession.EndSession(DateTimeOffset.FromUnixTimeSeconds(data.Time));

        HideAndSeekMode.GameManager.StopGame(CurrentSession, data.Aborted);

        Plugin.L.LogDebug($"Session has ended. {CurrentSession.FinalTime}");
    }

    public static void SendEpicTracer(Vector3 origin, Vector3 hitPoint)
    {
        var data = new pEpicTracer
        {
            Origin = origin,
            Destination = hitPoint,
        };
        
        NetworkingManager.SendEventAndInvokeLocally(data);
    }
    
    private static void OnEpicTracerReceived(ulong sender, pEpicTracer data)
    {
        CoroutineManager.StartCoroutine(EpicTracer.EpicTracerRoutine(data).WrapToIl2Cpp());
    }

    public static void SendMineAction(MineDeployerInstance mine, MineState state)
    {
        var data = new pMineAction
        {
            mineReplicatorKey = mine.Replicator.Key,
            state = (byte) state,
        };
        
        NetworkingManager.SendEventAndInvokeLocally(data);
    }
    
    private static void OnMineActionReceived(ulong sender, pMineAction data)
    {
        NetworkingManager.GetPlayerInfo(sender, out var info);

        var mine = ToolInstanceCaches.MineCache.All.FirstOrDefault(mine => mine?.Replicator?.Key == data.mineReplicatorKey);

        if (mine == null)
            return;

        var action = (MineState) data.state;

        CustomMineController.ProcessIncomingAction(info, mine, action);
    }

    public static void SendXRayAction(Vector3 origin, Vector3 direction)
    {
        var data = new pXRayAction
        {
            Position = origin,
            Direction = direction,
        };
        
        NetworkingManager.SendEvent(data, invokeLocal: true);
    }
    
    private static void OnXRayActionReceived(ulong sender, pXRayAction data)
    {
        XRayManager.OnXRayDataReceived(data);
    }
}
