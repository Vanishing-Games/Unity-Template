using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class VgSplashEntry
    {
        public VgSplashKey Key;
        public VgSplasher Splasher;
    }

    public class VgLoadingSplashManager
        : CoreModuleManagerBase<VgLoadingSplashManager>,
            ICoreModuleSystem
    {
        public string SystemName => "VgLoadingSplashManager";
        public Type[] Dependencies => Array.Empty<Type>();

        public void RegisterHooks(IGameCoreHookRegistry registry)
        {
            registry.OnBootStart(async () =>
            {
                foreach (var entry in m_Splashers)
                    if (entry.Splasher != null)
                        entry.Splasher.gameObject.SetActive(false);

                if (m_FallbackSplasher != null)
                    m_FallbackSplasher.gameObject.SetActive(false);

                Instance
                    .CoverAsync(VgSplashKey.GameLoading)
                    .ContinueWith(async () =>
                    {
                        await UniTask.Delay(4000);
                    })
                    .ContinueWith(() =>
                    {
                        Instance.RevealAsync(VgSplashKey.GameLoading).Forget();
                    })
                    .Forget();
            });
        }

        public async UniTask CoverAsync(VgSplashKey key, CancellationToken ct = default)
        {
            if (m_IsAnimating)
            {
                CLogger.LogWarn(
                    $"[VgLoadingSplashManager] CoverAsync({key}) called while animation is in progress — ignored.",
                    LogTag.Loading
                );
                return;
            }

            var splasher = FindSplasher(key);
            if (splasher == null)
                return;

            CLogger.LogInfo(
                $"[VgLoadingSplashManager] CoverAsync({key}) → {splasher.GetType().Name}",
                LogTag.Loading
            );

            m_IsAnimating = true;
            m_ActiveSplasher = splasher;
            splasher.gameObject.SetActive(true);

            try
            {
                await splasher.CoverAsync(ct);
            }
            finally
            {
                m_IsAnimating = false;
            }
        }

        public async UniTask RevealAsync(VgSplashKey key, CancellationToken ct = default)
        {
            if (m_IsAnimating)
            {
                CLogger.LogWarn(
                    $"[VgLoadingSplashManager] RevealAsync({key}) called while animation is in progress — ignored.",
                    LogTag.Loading
                );
                return;
            }

            var splasher = m_ActiveSplasher ?? FindSplasher(key);
            if (splasher == null)
                return;

            CLogger.LogInfo(
                $"[VgLoadingSplashManager] RevealAsync({key}) → {splasher.GetType().Name}",
                LogTag.Loading
            );

            m_IsAnimating = true;

            try
            {
                await splasher.RevealAsync(ct);
            }
            finally
            {
                splasher.gameObject.SetActive(false);
                m_IsAnimating = false;
                m_ActiveSplasher = null;
            }
        }

        public void UpdateProgress(float progress)
        {
            if (m_ActiveSplasher == null)
            {
                CLogger.LogWarn(
                    "[VgLoadingSplashManager] UpdateProgress called with no active Splasher.",
                    LogTag.Loading
                );
                return;
            }
            m_ActiveSplasher.UpdateProgress(progress);
        }

        private VgSplasher FindSplasher(VgSplashKey key)
        {
            foreach (var entry in m_Splashers)
            {
                if (entry.Key == key && entry.Splasher != null)
                    return entry.Splasher;
            }

            CLogger.LogWarn(
                $"[VgLoadingSplashManager] No Splasher found for key '{key}' — using fallback.",
                LogTag.Loading
            );

            if (m_FallbackSplasher == null)
                CLogger.LogError(
                    "[VgLoadingSplashManager] Fallback Splasher is null! Configure one in the Inspector.",
                    LogTag.Loading
                );

            return m_FallbackSplasher;
        }

        [SerializeField]
        private List<VgSplashEntry> m_Splashers = new();

        [SerializeField]
        private VgSplasher m_FallbackSplasher;

        private bool m_IsAnimating;
        private VgSplasher m_ActiveSplasher;
    }
}
