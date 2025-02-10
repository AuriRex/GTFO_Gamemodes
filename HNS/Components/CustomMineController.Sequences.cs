using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Core;
using HNS.Core;
using Il2CppInterop.Runtime.Attributes;
using Player;
using UnityEngine;

namespace HNS.Components;

public partial class CustomMineController
{
    [HideFromIl2Cpp]
    private IEnumerator AlarmSequence(PlayerAgent target)
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
        
        CoroutineManager.StartCoroutine(Coroutines.PlaceNavmarkerAtPos(target.Position + Vector3.up, "<color=orange><b>Motion Detected!</b></color>", color, 3f).WrapToIl2Cpp());
        
        for (int i = 0; i < 3; i++)
        {
            Sound.Stop();
            Sound.Post(AK.EVENTS.HACKING_PUZZLE_LOCK_ALARM, transform.position);
            yield return new WaitForSeconds(0.75f);
        }

        //yield return new WaitForSeconds(0.75f);
        
        _currentState = MineState.Disabled;
        RefreshVisuals();
    }

    [HideFromIl2Cpp]
    private IEnumerator HackSequence(PlayerAgent hacker)
    {
        float disableDuration = 15f;
        
        DisableMineForSeconds(disableDuration);

        _currentState = MineState.Hacked;
        RefreshVisuals();

        var beepCount = 12;
        var waitTime = disableDuration / beepCount;
        
        for (int i = 0; i < beepCount; i++)
        {
            Sound.Stop();
            Sound.Post(AK.EVENTS.HACKING_PUZZLE_WRONG, transform.position);
            yield return new WaitForSeconds(waitTime);
        }
        
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