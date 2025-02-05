using System;
using Gamemodes.Core;
using Gamemodes.Net;
using Player;
using UnityEngine;

namespace ExtraModes.GeoGuesser;

public class GGUpdaterComp : MonoBehaviour
{
    private float _nextUpdateTime;
    
    public void Update()
    {
        if (!GGMode.IsGameActive)
            return;

        var timeSinceStartup = Time.realtimeSinceStartup;

        if (_nextUpdateTime <= timeSinceStartup)
        {
            _nextUpdateTime = timeSinceStartup + 1f;

            DoUpdate();
        }
    }

    private void DoUpdate()
    {
        if (GamemodeManager.CurrentModeId != GGMode.MODE_ID)
        {
            GGMode.IsGameActive = false;
            Destroy(this);
            return;
        }
        
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player.IsLocallyOwned)
                continue;

            if (Vector3.Distance(transform.position, player.Position) > 2f)
                continue;
            
            NetworkingManager.GetPlayerInfo(player.Owner, out var info);

            if (info.Team == (int)GGTeams.HiddenOne)
                continue;
            
            NetworkingManager.AssignTeam(info, (int) GGTeams.HiddenOne);
        }
    }
}