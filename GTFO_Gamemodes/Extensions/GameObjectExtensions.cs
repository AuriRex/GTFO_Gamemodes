using System.Collections.Generic;
using UnityEngine;

namespace Gamemodes.Extensions;

public static class GameObjectExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : MonoBehaviour
    {
        var comp = gameObject.GetComponent<T>();

        if (comp != null)
        {
            return comp;
        }

        return gameObject.AddComponent<T>();
    }

    public static bool TryGetComponentButDontCrash<T>(this GameObject gameObject, out T comp) where T : MonoBehaviour
    {
        comp = gameObject.GetComponent<T>();

        return comp != null;
    }

    public static void SafeDestroy(this Object obj)
    {
        if (obj == null)
            return;

        Object.Destroy(obj);
    }
    
    public static void SafeDestroyGameObject(this Component component)
    {
        if (component == null)
            return;

        Object.Destroy(component.gameObject);
    }
    
    public static IEnumerable<Transform> Children(this GameObject gameObject) => Children(gameObject.transform);
    
    public static IEnumerable<Transform> Children(this Transform transform)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            yield return transform.GetChild(i);
        }
    }
    
    public static void DontDestroyAndSetHideFlags(this Object obj)
    {
        Object.DontDestroyOnLoad(obj);
        obj.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
    }
}
