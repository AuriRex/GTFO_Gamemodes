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
    
    public static void DontDestroyAndSetHideFlags(this UnityEngine.Object obj)
    {
        UnityEngine.Object.DontDestroyOnLoad(obj);
        obj.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
    }
}
