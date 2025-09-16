#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Core
{
    public partial class GameCore : MonoSingletonPersistent<GameCore>
    {
        private static readonly string[] UnityLifecycleMethods =
        {
            "Awake",
            "Start",
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnEnable",
            "OnDisable",
            "OnDestroy",
        };

        private bool GameRunInEditorCheck()
        {
            bool hasUnityLifecycleMethods = AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null);
                    }
                })
                .Where(t =>
                    t != null
                    && t.BaseType != null
                    && t.BaseType.IsGenericType
                    && t.BaseType.GetGenericTypeDefinition() == typeof(CoreModuleManagerBase<>)
                )
                .Any(t =>
                    t.GetMethods(
                            BindingFlags.Instance
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.DeclaredOnly
                        )
                        .Any(m => UnityLifecycleMethods.Contains(m.Name))
                );

            if (hasUnityLifecycleMethods)
            {
                Logger.EditorLogError(
                    $"Game Running Condition failed, beacause unity lifecycle methods are found in class inherit from CoreModuleManagerBase",
                    LogTag.GameRunCheck
                );
            }

            return !hasUnityLifecycleMethods;
        }
    }
}


#endif
