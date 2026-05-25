using System;
using System.Collections.Generic;
using Core;
using Newtonsoft.Json.Linq;
using R3;
using UnityEngine;

namespace GameMain.RunTime
{
    public class LevelStateRegistry : MonoSingletonPersistent<LevelStateRegistry>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            _ = Instance;
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            MessageBroker
                .Global.Subscribe<SaveSystemEvents.SavePreWriteEvent>(OnBeforeWrite)
                .AddTo(ref m_Disposables);
            MessageBroker
                .Global.Subscribe<SaveSystemEvents.SaveOnLoadEvent>(OnLoadSave)
                .AddTo(ref m_Disposables);
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
        }

        public void Register(string stableId, Func<object> capture, Action<JToken> restore)
        {
            if (string.IsNullOrEmpty(stableId))
                return;

            m_Entries[stableId] = new Entry(capture, restore);

            if (m_LoadedStates != null && m_LoadedStates.TryGetValue(stableId, out var saved))
            {
                try
                {
                    restore?.Invoke(saved);
                }
                catch (Exception e)
                {
                    CLogger.LogError(
                        $"[LevelStateRegistry] Restore failed for {stableId}: {e.Message}",
                        LogTag.Game
                    );
                }
            }
        }

        public void Unregister(string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
                return;
            m_Entries.Remove(stableId);
        }

        private void OnBeforeWrite(SaveSystemEvents.SavePreWriteEvent evt)
        {
            if (evt.IsGlobal)
                return;

            var snapshot = new Dictionary<string, object>();
            foreach (var kv in m_Entries)
            {
                var captured = kv.Value.Capture?.Invoke();
                if (captured != null)
                    snapshot[kv.Key] = captured;
            }

            if (m_LoadedStates != null)
            {
                foreach (var kv in m_LoadedStates)
                {
                    if (!snapshot.ContainsKey(kv.Key))
                        snapshot[kv.Key] = kv.Value;
                }
            }

            VgSaveSystem.Instance.UpdateSaveValue(SaveKeys.LevelObjectStates, snapshot);
        }

        private void OnLoadSave(SaveSystemEvents.SaveOnLoadEvent evt)
        {
            if (evt.IsGlobal)
                return;

            m_LoadedStates = new Dictionary<string, JToken>();

            if (evt.Container?.Data == null)
                return;

            if (!evt.Container.Data.TryGetValue(SaveKeys.LevelObjectStates, out var raw))
                return;

            JObject jobj = raw as JObject;
            if (jobj == null && raw != null)
            {
                try
                {
                    jobj = JObject.FromObject(raw);
                }
                catch
                {
                    return;
                }
            }
            if (jobj == null)
                return;

            foreach (var prop in jobj.Properties())
            {
                m_LoadedStates[prop.Name] = prop.Value;
            }

            foreach (var kv in m_Entries)
            {
                if (m_LoadedStates.TryGetValue(kv.Key, out var token))
                {
                    try
                    {
                        kv.Value.Restore?.Invoke(token);
                    }
                    catch (Exception e)
                    {
                        CLogger.LogError(
                            $"[LevelStateRegistry] Restore failed for {kv.Key}: {e.Message}",
                            LogTag.Game
                        );
                    }
                }
            }
        }

        private class Entry
        {
            public readonly Func<object> Capture;
            public readonly Action<JToken> Restore;

            public Entry(Func<object> capture, Action<JToken> restore)
            {
                Capture = capture;
                Restore = restore;
            }
        }

        private readonly Dictionary<string, Entry> m_Entries = new();
        private Dictionary<string, JToken> m_LoadedStates;
        private DisposableBag m_Disposables;
    }
}
