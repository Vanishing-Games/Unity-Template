using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Core
{
    public class LoadManager : MonoSingletonLasy<LoadManager>
    {
        private void Awake()
        {
            RegisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

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
            LoadAsync().Forget();
        }

        private async UniTask LoadAsync()
        {
            try
            {
                MessageBroker.Global.Publish(new LoadProgressEvent("Loading Scenes..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.LoadScene();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Reading Resources..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.LoadResource();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Loading Prefabs..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.LoadPrefab();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Instantiating Prefabs..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.InstantiatePrefab();
                }

                MessageBroker.Global.Publish(new LoadProgressEvent("Initializing..."));
                foreach (var loader in m_Loaders)
                {
                    await loader.InitLoadedThings();
                }

                Reset();

                MessageBroker.Global.PublishComplete(new LoadProgressEvent("Loading Done"));
                MessageBroker.Global.Complete<LoadRequestEvent>();
            }
            catch (Exception ex)
            {
                Logger.ReleaseLogError(
                    $"Loading failed with exception: \n {ex.Message}",
                    LogTag.Loading
                );
                Reset();

                MessageBroker.Global.PublishErrorStop<LoadProgressEvent>(this, ex);
                MessageBroker.Global.PublishErrorStop<LoadRequestEvent>(this, ex);
            }
        }

        private void RegisterEvents() { }

        private void UnregisterEvents() { }

        private List<ILoader> m_Loaders = new();
        private List<ILoadInfo> m_LoadInfos = new();
    }
}
