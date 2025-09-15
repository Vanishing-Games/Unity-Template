using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core
{
    public class LoadRequestEvent : IEvent
    {
        public struct LoadSettings
        {
            public UInt32 maxWaitTimeInMs;

            public static LoadSettings Default => new LoadSettings { maxWaitTimeInMs = 1000 };
        }

        public LoadRequestEvent(string loadEventType)
        {
            m_LoadEventType = loadEventType;
        }

        public LoadRequestEvent(
            string loadEventType,
            List<ILoadInfo> loadInfo,
            LoadSettings loadSettings
        )
        {
            m_LoadEventType = loadEventType;
            m_LoadInfos = loadInfo;
            m_LoadSettings = loadSettings;
        }

        public void Set(LoadSettings loadSettings)
        {
            m_LoadSettings = loadSettings;
        }

        public void AddLoadInfo(ILoadInfo loadInfo)
        {
            m_LoadInfos.Add(loadInfo);
        }

        public ILoadInfo GetLoadInfo(LoaderType loaderType)
        {
            return m_LoadInfos.FirstOrDefault(loadInfo =>
                loadInfo.GetNeededLoaderType() == loaderType
            );
        }

        public List<ILoadInfo> m_LoadInfos { get; private set; } = new();

        public LoadSettings m_LoadSettings { get; private set; } = LoadSettings.Default;

        public string m_LoadEventType { get; private set; } = string.Empty;
    }

    public class LoadStartEvent : IEvent { }

    public class LoadFinishEvent : IEvent { }

    public class LoadProgressEvent : IEvent
    {
        public LoadProgressEvent(string progress)
        {
            Progress = progress;
        }

        public string GetProgress()
        {
            return Progress;
        }

        public string Progress { get; private set; }
    }

    public class LoadErrorEvent : IEvent { }
}
