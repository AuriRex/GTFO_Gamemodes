using System;
using System.Collections;
using Gamemodes.Extensions;
using UnityEngine;

namespace Gamemodes.Core;

public static class Coroutines
{
    public static IEnumerator PlaceNavmarkerAtPos(Vector3 position, string message, Color color, float displayDuration, float fadeoutDuration = 5f)
    {
        if (displayDuration == 0 && fadeoutDuration == 0)
            yield break;
        
        var go = new GameObject("HNS_Temp_Highlight_GO");
        go.transform.position = position;
        
        var marker = go.AddComponent<PlaceNavMarkerOnGO>();

        marker.type = PlaceNavMarkerOnGO.eMarkerType.Waypoint;
        marker.m_nameToShow = message;
        
        marker.PlaceMarker();
        marker.UpdatePlayerColor(color);
        marker.SetMarkerVisible(true);
        
        if (displayDuration > 0)
            yield return new WaitForSeconds(displayDuration);

        if (fadeoutDuration > 0)
        {
            marker.m_marker.FadeOut(0f, fadeoutDuration);
        
            yield return new WaitForSeconds(fadeoutDuration + 0.1f);
        }
        
        marker.SafeDestroyGameObject();
    }

    public static IEnumerator DoAfter(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }

    public static IEnumerator NextFrame(Action action)
    {
        yield return null;
        action?.Invoke();
    }
}