using System;
using System.Collections;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Components;
using Gamemodes.Core;
using Gamemodes.Net;
using Gamemodes.UI;
using Gear;
using HNS.Components;
using HNS.Net;
using Player;
using SNetwork;
using UnityEngine;

namespace HNS.Core;

public class HideAndSeekGameManager
{
    public HideAndSeekGameManager(TimerHUD timer, TimeKeeper timeKeeper = null)
    {
        SetTimerHud(timer);
        SetTimeKeeper(timeKeeper);
    }

    private Blinds _blinds;

    private Coroutine _unblindPlayerCoroutine;
    private Coroutine _ammoTickCoroutine;

    private int _elapsedAmmoTicks;
    private bool ShouldSeekersHaveInfiniteAmmo => _elapsedAmmoTicks >= AmmoTickInfiniteAmmoAtTick;

    private bool _localPlayerIsSeeker;
    private bool _startedAsSeeker;

    private Session _session;

    private TimerHUD _gameTimerDisplay;
    private TimeKeeper _timeKeeper;

    private const string GAMESTART_COUNTDOWN_FORMATTEXT = $"{TimerHUD.COUNTDOWN_TIMER_MARKER} until seekers are released.";
    
    private static readonly float AmmoTickInterval = 300f;
    private static readonly int AmmoTickInfiniteAmmoAtTick = 5;
    
    public void SetTimerHud(TimerHUD timer)
    {
        _gameTimerDisplay = timer;
    }

    public void SetTimeKeeper(TimeKeeper timeKeeper)
    {
        _timeKeeper = timeKeeper;
    }
    
    public void StartGame(bool localPlayerIsSeeker, byte blindDuration, Session session)
    {
        if (_session != null && _session.IsActive)
        {
            _session.EndSession();
            StopGame(_session, aborted: true);
        }

        if (NetworkingManager.LocalPlayerTeam != (int) GMTeam.Camera)
            SpectatorController.TryExit();
        
        PlayerTrackerController.GetCooldownDuration = GetCooldownDuration;
        
        _session = session;
        _gameTimerDisplay.ResetGameTimer();
        _gameTimerDisplay.StartGameTimer();
        _gameTimerDisplay.StartCountdown(blindDuration, GAMESTART_COUNTDOWN_FORMATTEXT);

        _localPlayerIsSeeker = localPlayerIsSeeker;
        _startedAsSeeker = localPlayerIsSeeker;

        GamemodeBase.InstantReviveLocalPlayer();

        Utils.SetLocalPlayerInfection(0f);

        _elapsedAmmoTicks = 0;
        
        Blinds blinds = null;
        if (localPlayerIsSeeker)
        {
            blinds = BlindPlayer();
            EquipSniper();
        }
        
        var bioTracker = GearManager.GetAllPlayerGear().FirstOrDefault(g => g.PublicGearName.Contains("Optron"));
        GearUtils.EquipGear(bioTracker, InventorySlot.GearClass);

        SetPlayerAmmo();

        _unblindPlayerCoroutine = CoroutineManager.StartCoroutine(GameStartSetupTimeCoroutine(blindDuration, blinds).WrapToIl2Cpp());
        _ammoTickCoroutine = CoroutineManager.StartCoroutine(AmmoTickCoroutine().WrapToIl2Cpp());
        
        Utils.LocallyResetAllWeakDoors();

        if (SNet.IsMaster)
        {
            HideAndSeekMode.StartFlashSpawnerRoutine();
            HideAndSeekMode.DespawnMineInstancesAndCFoamBlobs();
        }
    }

    private float GetCooldownDuration()
    {
        if (_session == null || !_session.IsActive)
        {
            return PlayerTrackerController.CooldownDuration;
        }
        
        var progress = Math.Min(0f, Math.Max(_elapsedAmmoTicks, 5f));

        var cooldownLoss = PlayerTrackerController.CooldownDuration / 6f;

        return Math.Max(15f, PlayerTrackerController.CooldownDuration - cooldownLoss * progress);
    }

    private IEnumerator AmmoTickCoroutine()
    {
        yield return new WaitForSeconds(5f);

        _elapsedAmmoTicks = 0;
        
        while (true)
        {
            var yielder = new WaitForSeconds(AmmoTickInterval);
            yield return yielder;

            _elapsedAmmoTicks++;
            
            if (_session == null || !_session.IsActive)
            {
                break;
            }
            
            if (ShouldSeekersHaveInfiniteAmmo)
            {
                _gameTimerDisplay.StartCountdown(10, $"All Seekers have received <b><color=purple>Infinite Reserve Ammo</color></b> for the remainder of the round!", () => TimerHUD.TSO_RED_WARNING_BLINKING);

                if (_localPlayerIsSeeker)
                {
                    GearUtils.LocalReserveAmmoAction(GearUtils.AmmoType.Guns, GearUtils.AmmoAction.Fill);
                    GearUtils.LocalGunClipAction(GearUtils.AmmoAction.Fill);
                }
                yield break;
            }
            
            if (_localPlayerIsSeeker)
            {
                AddSniperBullet();
                _gameTimerDisplay.StartCountdown(5, $"Received <b><color=orange>1</color></b> Sniper Bullet!", () => TimerHUD.TSO_GREEN_BLINKING);
            }
        }
    }
    
    private static void EquipSniper()
    {
        var gear = GearManager.GetAllPlayerGear().FirstOrDefault(g => g.PublicGearName.Contains("Köning"));
        GearUtils.EquipGear(gear, InventorySlot.GearSpecial);
    }

    private void SetPlayerAmmo()
    {
        var action = GearUtils.AmmoAction.Fill;
        if (_localPlayerIsSeeker && !ShouldSeekersHaveInfiniteAmmo)
        {
            action = GearUtils.AmmoAction.Empty;
        }
        
        GearUtils.LocalReserveAmmoAction(GearUtils.AmmoType.Guns, action);
        GearUtils.LocalGunClipAction(action);
        
        GearUtils.LocalReserveAmmoAction(GearUtils.AmmoType.Tool, GearUtils.AmmoAction.SetToPercent, 0.125f);
    }

    private void AddSniperBullet()
    {
        GearUtils.LocalGunClipAction(InventorySlot.GearSpecial, GearUtils.AmmoAction.AddPercent, 0.5f);
        //GearUtils.LocalGunClipAction(InventorySlot.GearSpecial, GearUtils.AmmoAction.ClampToMinMax);
    }
    
    public bool OnLocalPlayerCaught()
    {
        if (_session == null || !_session.IsActive)
            return false;

        if (!_localPlayerIsSeeker)
        {
            _localPlayerIsSeeker = true;
            _session.LocalPlayerCaught();
            
            var hiddenForMsg = $"<color=orange>Time spent hiding: {_session.HidingTime.ToString(@"mm\:ss")}</color>";
            _gameTimerDisplay.StartCountdown(5, $"You've been caught!\n{hiddenForMsg}\nReviving in {TimerHUD.COUNTDOWN_TIMER_MARKER}", StyleRed);
            Gamemodes.Plugin.PostLocalMessage(hiddenForMsg);
            EquipSniper();
            SetPlayerAmmo();
            HideAndSeekMode.SetToolAmmoForLocalPlayer();
            
            Utils.SetLocalPlayerInfection(0f);
            
            AddSniperBullet();
            
            DetonateAllLocalMineInstances();
        }
        else
        {
            _gameTimerDisplay.StartCountdown(5, $"Reviving in {TimerHUD.COUNTDOWN_TIMER_MARKER}", StyleRed);
        }

        CoroutineManager.StartCoroutine(RevivePlayerRoutine(5).WrapToIl2Cpp());
        return true;
    }

    private void DetonateAllLocalMineInstances()
    {
        foreach (var mine in ToolInstanceCaches.MineCache.All)
        {
            if (!mine.LocallyPlaced)
                continue;
            
            mine.WantItemAction(null, SyncedItemAction_New.Trigger);
        }
    }

    public void ReviveLocalPlayer(int reviveDelay = 5, bool showSeekerMessage = false)
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return;

        if (localPlayer.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.Downed)
            return;

        _gameTimerDisplay.StartCountdown(reviveDelay, $"Reviving in {TimerHUD.COUNTDOWN_TIMER_MARKER}", StyleDefault);

        CoroutineManager.StartCoroutine(RevivePlayerRoutine(5, showSeekerMessage).WrapToIl2Cpp());
    }

    private IEnumerator RevivePlayerRoutine(int reviveDelay, bool showSeekerMessage = true)
    {
        if (reviveDelay > 0)
        {
            var yielder = new WaitForSeconds(reviveDelay);
            yield return yielder;
        }

        if (!NetworkingManager.InLevel)
            yield break;

        GamemodeBase.InstantReviveLocalPlayer();

        if (_session != null && _session.IsActive && showSeekerMessage)
        {
            _gameTimerDisplay.StartCountdown(5, $"You've been caught!\nFind the remaining hiders!", StyleRed);
        }
    }

    internal void StopGame(Session session, bool aborted = false)
    {
        if (_session != session)
        {
            Plugin.L.LogWarning("Stopped session is not the same as the started one?? This should not happen!");
        }

        _gameTimerDisplay.StopGameTimer();
        
        if (_unblindPlayerCoroutine != null)
        {
            CoroutineManager.StopCoroutine(_unblindPlayerCoroutine);
            _unblindPlayerCoroutine = null;
        }

        if (_ammoTickCoroutine != null)
        {
            CoroutineManager.StopCoroutine(_ammoTickCoroutine);
            _ammoTickCoroutine = null;
        }

        _blinds?.Dispose();
        _blinds = null;

        Utils.SetLocalPlayerInfection(0f);
        
        GearUtils.LocalGunClipAction(GearUtils.AmmoAction.Fill);
        GearUtils.LocalReserveAmmoAction(GearUtils.AmmoType.Guns, GearUtils.AmmoAction.Fill);
        
        var message = $"Game Over! Total time: {session.FinalTime.ToString(@"mm\:ss")}";
        Gamemodes.Plugin.PostLocalMessage("<#0f0>-= Game Over! =-</color>");
        Gamemodes.Plugin.PostLocalMessage($"<color=white>Total Game Time: {session.FinalTime.ToString(@"mm\:ss")}</color>");
        Plugin.L.LogInfo($"Total Game Time: {session.FinalTime.ToString(@"mm\:ss")}");
        
        if (!_startedAsSeeker)
        {
            var hid = $"<color=orange>You hid for: {session.HidingTime.ToString(@"mm\:ss")}</color>";
            message = $"{message}\n{hid}";
            Gamemodes.Plugin.PostLocalMessage(hid);
            Plugin.L.LogInfo($"You hid for: {session.HidingTime.ToString(@"mm\:ss")}");
        }

        if (aborted)
        {
            Plugin.L.LogWarning($"Game was aborted!");
            message = "<color=red>Game aborted!</color>";
            Gamemodes.Plugin.PostLocalMessage(message);
            _gameTimerDisplay.StartCountdown(5, message, StyleRed);
            return;
        }

        _timeKeeper?.PushSession(session);
        
        _gameTimerDisplay.StartCountdown(10, message, StyleImportant);

        if (SNet.IsMaster)
        {
            CoroutineManager.StartCoroutine(PostGameTeamSwitchCoroutine().WrapToIl2Cpp());
        }
    }

    private IEnumerator PostGameTeamSwitchCoroutine()
    {
        if (!SNet.IsMaster)
            yield break;

        var session = _session;

        var yielder = new WaitForSeconds(5);
        yield return yielder;

        if (session != _session)
            yield break;

        foreach(var player in NetworkingManager.AllValidPlayers)
        {
            if (player.Team == (int)GMTeam.Camera)
                continue;

            NetworkingManager.AssignTeam(player, (int)TeamHelper.GetPreGameTeamForPlayer((GMTeam)player.Team));
        }
    }

    private TimerStyleOverride? StyleDefault()
    {
        return TimerHUD.TSO_DEFAULT;
    }
    
    private TimerStyleOverride? StyleImportant()
    {
        return TimerHUD.TSO_GREEN_BLINKING;
    }

    private TimerStyleOverride? StyleRed()
    {
        return TimerHUD.TSO_RED_WARNING_BLINKING;
    }

    private Blinds BlindPlayer()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return null;

        _blinds?.Dispose();
        _blinds = new(localPlayer.TryCast<LocalPlayerAgent>());
        return _blinds;
    }

    private IEnumerator GameStartSetupTimeCoroutine(byte setupDuration, Blinds blinds)
    {
        var yielder = new WaitForSeconds(setupDuration);
        yield return yielder;

        blinds?.Dispose();
        if (_blinds == blinds)
            _blinds = null;

        if (_localPlayerIsSeeker)
            AddSniperBullet();
        
        _gameTimerDisplay.StartCountdown(10, "Seekers have been released!", StyleImportant);
    }
}
