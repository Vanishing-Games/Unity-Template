using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class LoadManager : MonoSingletonLasy<LoadManager>
    {
        public void PrepareForLoad(LoadRequestEvent loadEvent)
        {
            m_LoadInfos.AddRange(loadEvent.m_LoadInfos);
        }

        public void RegisterLoader(ILoader newLoader)
        {
            Logger.DebugLogVerbose(
                $"Registering loader {newLoader.GetLoaderType()}",
                LogTag.Loading
            );

            bool isLoaderNeeded = false;
            foreach (var loadInfo in m_LoadInfos)
            {
                if (loadInfo.GetNeededLoaderType() == newLoader.GetLoaderType())
                {
                    isLoaderNeeded = true;
                    break;
                }
            }

            if (!isLoaderNeeded)
            {
                Logger.DebugLogWarn(
                    $"Loader {newLoader.GetLoaderType()} is not needed",
                    LogTag.Loading
                );
                return;
            }

            foreach (var loader in m_Loaders)
            {
                if (loader.GetLoaderType() == newLoader.GetLoaderType())
                {
                    Logger.DebugLogWarn(
                        $"Loader {loader.GetLoaderType()} already registed",
                        LogTag.Loading
                    );
                    return;
                }
            }

            m_Loaders.Add(newLoader);
            Logger.ReleaseLogInfo($"Registed loader {newLoader.GetLoaderType()}", LogTag.Loading);

            if (m_Loaders.Count == m_LoadInfos.Count)
            {
                Logger.ReleaseLogInfo("All loaders registered, starting loading", LogTag.Loading);
                Load();
            }
        }

        private void Reset()
        {
            m_Loaders.Clear();
            m_LoadInfos.Clear();
        }

        private void Load()
        {
            MessageBroker.Global.PublishComplete(new LoadStartEvent());
            StartCoroutine(Loading());
        }

        private IEnumerator Loading()
        {
            MessageBroker.Global.Publish(new LoadProgressEvent("Loading Scenes..."));
            foreach (var loader in m_Loaders)
            {
                yield return StartCoroutine(loader.LoadScene());
            }

            MessageBroker.Global.Publish(new LoadProgressEvent("Reading Resources..."));
            foreach (var loader in m_Loaders)
            {
                yield return StartCoroutine(loader.LoadResource());
            }

            MessageBroker.Global.Publish(new LoadProgressEvent("Loading Prefabs..."));
            foreach (var loader in m_Loaders)
            {
                yield return StartCoroutine(loader.LoadPrefab());
            }

            MessageBroker.Global.Publish(new LoadProgressEvent("Instantiating Prefabs..."));
            foreach (var loader in m_Loaders)
            {
                yield return StartCoroutine(loader.InstantiatePrefab());
            }

            MessageBroker.Global.Publish(new LoadProgressEvent("Initializing..."));
            foreach (var loader in m_Loaders)
            {
                yield return StartCoroutine(loader.InitLoadedThings());
            }

            Reset();

            MessageBroker.Global.PublishComplete(new LoadProgressEvent("Loading Done"));
            MessageBroker.Global.PublishComplete(new LoadFinishEvent());
        }

        private List<ILoader> m_Loaders = new();
        private List<ILoadInfo> m_LoadInfos = new();
    }
}
