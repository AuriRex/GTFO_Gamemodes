using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Core;
using Gamemodes.Net;
using HNS.Core;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace HNS.Components;

public partial class CustomMineController
{
    [HideFromIl2Cpp]
    private IEnumerator AlarmSequence(PlayerWrapper target)
    {
        float disableDuration = 3f;
        if (_owningTeam == GMTeam.Hiders)
            disableDuration = 30f;
        
        DisableMineForSeconds(disableDuration);

        _currentState = MineState.Alarm;
        RefreshVisuals();

        var color = Color.red;
        if (_owningTeam == GMTeam.Seekers)
            color = Color.cyan;
        
        CoroutineManager.StartCoroutine(Coroutines.PlaceNavmarkerAtPos(target.PlayerAgent.Position + Vector3.up, "<color=orange><b>Motion Detected!</b></color>", color, 3f).WrapToIl2Cpp());

        var sound = GetSoundPlayer();
        sound.Activate();
        
        for (int i = 0; i < 3; i++)
        {
            sound.Stop();
            sound.Post(AK.EVENTS.HACKING_PUZZLE_LOCK_ALARM, _mine.transform.position);
            yield return new WaitForSeconds(0.75f);
        }

        sound.Deactivate();

        //yield return new WaitForSeconds(0.75f);
        
        _currentState = MineState.Disabled;
        RefreshVisuals();
    }

    [HideFromIl2Cpp]
    private IEnumerator HackSequence(PlayerWrapper hacker)
    {
        float disableDuration = 15f;
        
        DisableMineForSeconds(disableDuration);

        _currentState = MineState.Hacked;
        RefreshVisuals();

        var beepCount = 12;
        var waitTime = disableDuration / beepCount;
        
        var sound = GetSoundPlayer();
        sound.Activate();
        
        for (int i = 0; i < beepCount; i++)
        {
            sound.Stop();
            sound.Post(AK.EVENTS.HACKING_PUZZLE_WRONG, _mine.transform.position);
            yield return new WaitForSeconds(waitTime);
        }
        
        sound.Deactivate();
        
        if (_mine.LocallyPlaced)
            CoroutineManager.StartCoroutine(Coroutines.PlaceNavmarkerAtPos(_mine.transform.position, "<color=red><b>Device Rebooted\nafter Hack!</b></color>", Color.red, 3f).WrapToIl2Cpp());
        
        _currentState = MineState.Detecting;
        RefreshVisuals();
    }

    [HideFromIl2Cpp]
    private IEnumerator EvilSequence()
    {
        yield return new WaitForSeconds(5f);
        
        _mine.WantItemAction(null, SyncedItemAction_New.Trigger);
    }
}