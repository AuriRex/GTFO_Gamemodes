using CellMenu;
using LevelGeneration;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Gamemodes.Mode;

public static class Utils
{
    public static void StopWardenObjectiveManager()
    {
        var WOM = WardenObjectiveManager.Current;

        WardenObjectiveManager.OnLocalPlayerEnterZoneCallback = null;
        WardenObjectiveManager.OnLocalPlayerEnterNewLayerCallback = null;


        WOM.CleanupWaveTriggers();

        foreach (IWardenObjective wardenObjective in WOM.m_wardenObjectives.Values)
        {
            wardenObjective.OnLevelCleanup();
        }
        WOM.m_wardenObjectives.Clear();

        //WardenObjectiveManager.m_expeditionStarted = false;
        WardenObjectiveManager.m_customGeoExitWinConditionItem = null;
        WardenObjectiveManager.m_elevatorExitWinConditionItem = null;
        //WardenObjectiveManager.m_exitWaveTriggered = false;
        WardenObjectiveManager.m_exitEventsTriggered = false;

        WOM.SetupContainers();
        //WOM.m_stateReplicator.SetStateUnsynced(WardenObjectiveManager.m_startState);
        WOM.ProgressionObjectiveManager.OnLevelCleanup();

        WOM.StopAllCoroutines();
        WOM.TryCast<IWardenObjectiveContext>().StopAllWardenObjectiveEnemyWaves();
    }

    public static void DisableAllWorldEventTriggers()
    {
        int count = DisableWEComponent<LG_CollisionWorldEventTrigger>();
        count += DisableWEComponent<LG_InteractWorldEventTrigger>();
        count += DisableWEComponent<LG_LookatWorldEventTrigger>();

        Plugin.L.LogDebug($"Disabled {count} World Event Triggers.");
    }

    private static int DisableWEComponent<TGame>() where TGame : MonoBehaviour
    {
        var eventObjects = WorldEventManager.Current.m_worldEventObjects;

        int count = 0;
        foreach (var lt in eventObjects)
        {
            if (lt.GetComponent<TGame>() == null)
                continue;

            lt.gameObject.SetActive(false);
            count++;
        }

        return count;
    }

    #region MapReveal
    public const MapIconTypes EVERYTHING_EXCEPT_LOCKERS = MapIconTypes.SecurityDoors | MapIconTypes.WeakDoors
        | MapIconTypes.Ladders | MapIconTypes.ZoneSigns | MapIconTypes.Terminals | MapIconTypes.BulkheadDCs
        | MapIconTypes.DisinfectionStations | MapIconTypes.PowerGenerators;

    public const MapIconTypes EVERYTHING = EVERYTHING_EXCEPT_LOCKERS | MapIconTypes.ResourceLockers;

    [Flags]
    public enum MapIconTypes
    {
        None = 0,
        SecurityDoors = 1 << 0,
        WeakDoors = 1 << 1,
        Ladders = 1 << 2,
        ZoneSigns = 1 << 3,
        Terminals = 1 << 4,
        BulkheadDCs = 1 << 5,
        DisinfectionStations = 1 << 6,
        PowerGenerators = 1 << 7,
        ResourceLockers = 1 << 8,
    }

    public static void RevealMap(bool revealed = true)
    {
        if (!MapDetails.s_isSetup)
            return;

        Material uiMaterial = MapDetails.Current.m_UIMaterial;

        if (uiMaterial == null)
            return;

        Vector4 vector = uiMaterial.GetVector("_Settings");
        if (revealed)
        {
            vector.w = 1f;
        }
        else
        {
            vector.w = 0f;
        }
        uiMaterial.SetVector("_Settings", vector);

        var mainMap = CM_PageMap.Current.m_mapDetails.m_UIObject;
        var trans = CM_PageMap.Current.m_mapMover.transform;
        for (int i = 0; i < trans.childCount; i++)
        {
            var child = trans.GetChild(i);

            if (child.name != "MapDetailsUI(Clone)")
                continue;

            if (child.gameObject == mainMap)
                continue;

            child.gameObject.SetActive(false);
        }
    }

    public static void RevealMapIcons(MapIconTypes thingsToReveal)
    {

        if ((thingsToReveal & MapIconTypes.SecurityDoors) != 0)
        {
            var secDoors = UnityEngine.Object.FindObjectsOfType<LG_SecurityDoor>();
            foreach (var door in secDoors)
            {
                door.GetComponentInChildren<LG_DoorMapLookatRevealer>()?.OnCamHoverEnter(null);
            }
        }

        if ((thingsToReveal & MapIconTypes.WeakDoors) != 0)
        {
            var weakDoors = UnityEngine.Object.FindObjectsOfType<LG_WeakDoor>();
            foreach (var door in weakDoors)
            {
                door.GetComponentInChildren<LG_DoorMapLookatRevealer>()?.OnCamHoverEnter(null);
            }
        }

        if ((thingsToReveal & MapIconTypes.WeakDoors) != 0)
        {
            var ladders = UnityEngine.Object.FindObjectsOfType<LG_Ladder>().Where(ladder => ladder.m_enemyClimbingOnly == false);

            foreach (var ladder in ladders)
            {
                ladder.GetComponent<LG_LadderMapLookatRevealer>()?.OnCamHoverEnter(null);
            }
        }

        foreach (var zone in CM_PageMap.Current.m_zoneGUI)
        {
            foreach (var area in zone.m_areaGUIs)
            {
                ProcessMapIconsArea(area, thingsToReveal);
            }
        }
    }

    private static void ProcessMapIconsArea(CM_MapAreaGUIItem area, MapIconTypes thingsToReveal)
    {
        if ((thingsToReveal & MapIconTypes.ZoneSigns) != 0)
        {
            foreach (var item in area.m_signGUIs)
            {
                item.ForceVisible();
                item.SetTextVisible(item.m_locatorTxt, true);
                item.SetTextVisible(item.m_additionalTxt, true);
            }
        }

        if ((thingsToReveal & MapIconTypes.Terminals) != 0)
        {
            foreach (var item in area.m_computerTerminalGUIs)
            {
                item.ForceVisible();
            }
        }

        if ((thingsToReveal & MapIconTypes.BulkheadDCs) != 0)
        {
            foreach (var item in area.m_bulkheadDoorControllerGUIs)
            {
                item.ForceVisible();
            }
        }

        if ((thingsToReveal & MapIconTypes.DisinfectionStations) != 0)
        {
            foreach (var item in area.m_disinfectionStationGUIs)
            {
                item.ForceVisible();
            }
        }

        if ((thingsToReveal & MapIconTypes.PowerGenerators) != 0)
        {
            foreach (var item in area.m_powerGeneratorGUIs)
            {
                item.ForceVisible();
            }
        }

        if ((thingsToReveal & MapIconTypes.ResourceLockers) != 0)
        {
            foreach (var item in area.m_resourceLockerGUIs)
            {
                item.ForceVisible();
            }
            foreach (var item in area.m_resourceBoxGUIs)
            {
                item.ForceVisible();
            }
        }
    }
    #endregion

    #region Open_Doors
    public static IEnumerator OpenSecurityDoorRoutine()
    {
        var secDoors = UnityEngine.Object.FindObjectsOfType<LG_SecurityDoor>();

        foreach (var secDoor in secDoors)
        {
            secDoor.m_sync.AttemptDoorInteraction(eDoorInteractionType.Open, 0, 0, Vector3.zero, null);
            yield return null;
        }
    }

    public static IEnumerator OpenWeakDoorRoutine()
    {
        yield return null;

        var weakDoors = UnityEngine.Object.FindObjectsOfType<LG_WeakDoor>();

        int c = 0;
        foreach (var door in weakDoors)
        {
            if (door.LastStatus == eDoorStatus.Closed)
                door.AttemptOpenCloseInteraction();

            foreach (var btn in door.m_buttons)
            {
                var weakLock = btn.GetComponentInChildren<LG_WeakLock>();

                if (weakLock == null)
                    continue;

                weakLock.AttemptInteract(new pWeakLockInteraction
                {
                    damage = 0,
                    open = true,
                    type = eWeakLockInteractionType.Unlock,
                });

                c++;
            }

            c++;

            if (c >= 15)
            {
                c = 0;
                yield return null;
            }
        }
    }
    #endregion
}
