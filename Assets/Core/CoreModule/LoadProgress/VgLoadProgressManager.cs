using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Core
{
    public interface IProgressable
    {
        void UpdateProgress(float progress);
        void Tick();
        void Show();
        void Hide();
    }

    public abstract class MonoProgressable : MonoBehaviour, IProgressable
    {
        public abstract void UpdateProgress(float progress);

        public virtual void Tick() { }

        public abstract void Show();
        public abstract void Hide();
    }

    public class VgLoadProgressManager
        : CoreModuleManagerBase<VgLoadProgressManager, ProgressBarLoadInfo, ProgressBarLoader>,
            IDisposable
    {
        public void Init()
        {
            if (m_Inited)
                return;

            var progressables = FindObjectsByType<MonoProgressable>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var progressable in progressables)
                m_Progressables.Add(progressable);

            StringBuilder sb = new();
            sb.AppendLine("VgLoadProgressManager Init with Progressables:");
            foreach (var progressable in m_Progressables)
                sb.AppendLine($" - {progressable.GetType()}");
            Logger.LogInfo(sb.ToString(), LogTag.VgLoadProgressManager);

            m_Inited = true;
        }

        public void Show()
        {
            if (!m_Inited)
                Init();

            foreach (var progressable in m_Progressables)
                progressable.Show();

            m_Hided = false;
        }

        private void Tick()
        {
            if (m_Hided)
            {
                Logger.LogWarn(
                    "VgLoadProgressManager is hidden, but Tick is called",
                    LogTag.VgLoadProgressManager
                );
                Show();
            }

            foreach (var progressable in m_Progressables)
                progressable.Tick();
        }

        public void UpdateProgress(float progress)
        {
            if (m_Hided)
            {
                Logger.LogWarn(
                    "VgLoadProgressManager is hidden, but UpdateProgress is called",
                    LogTag.VgLoadProgressManager
                );
                Show();
            }

            if (progress < 0)
            {
                Logger.LogWarn(
                    "Progress is less than 0, which is not allowed",
                    LogTag.VgLoadProgressManager
                );
                return;
            }

            progress = Mathf.Clamp(progress, 0f, 1f);

            foreach (var progressable in m_Progressables)
                progressable.UpdateProgress(progress);

            m_Progress = progress;
        }

        public void AddProgress(float progress) => UpdateProgress(m_Progress + progress);

        public float GetProgress() => m_Progress;

        public void Hide()
        {
            foreach (var progressable in m_Progressables)
                progressable.Hide();

            m_Hided = true;
        }

        public void Dispose() => Hide();

        protected override LoaderType GetLoaderType() => LoaderType.ProgressBar;

        protected override void OnLoadingError(Exception exception) { }

        private bool m_Inited = false;
        private bool m_Hided = true;
        private List<IProgressable> m_Progressables = new();
        private float m_Progress = 0;
    }
}
