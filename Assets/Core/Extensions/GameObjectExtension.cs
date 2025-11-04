using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Extensions
{
    public static class GameObjectExtension
    {
        public static T GetOrAddComponentRecursively<T>(this GameObject gameObject)
            where T : Component
        {
            return gameObject.GetComponentInChildren<T>() ?? gameObject.AddComponent<T>();
        }

        public static T[] GetOrAddComponentsRecursively<T>(this GameObject gameObject)
            where T : Component
        {
            return gameObject.GetComponentsInChildren<T>() is { Length: > 0 } c
                ? c
                : new[] { gameObject.AddComponent<T>() };
        }
    }
}
