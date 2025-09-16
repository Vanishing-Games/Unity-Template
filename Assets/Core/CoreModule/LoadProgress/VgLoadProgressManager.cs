using System;
using System.Collections;
using System.Collections.Generic;
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

    public class VgLoadProgressManager
        : CoreModuleManagerBase<VgLoadProgressManager, ProgressBarLoadInfo, ProgressBarLoader>
    {
        public void Init()
        {
            if (m_Inited)
                return;

            m_Progressables.AddRange(transform.GetComponentsInChildren<IProgressable>());

            m_Inited = true;
        }

        public void Show()
        {
            foreach (var progressable in m_Progressables)
                progressable.Show();
        }

        public void Tick()
        {
            foreach (var progressable in m_Progressables)
            {
                progressable.Tick();
            }
        }

        public void UpdateProgress(float progress)
        {
            foreach (var progressable in m_Progressables)
            {
                progressable.UpdateProgress(progress);
            }
        }

        public void Hide()
        {
            foreach (var progressable in m_Progressables)
                progressable.Hide();
        }

        protected override LoaderType GetLoaderType() => LoaderType.ProgressBar;

        protected override void OnLoadingError(Exception exception) { }

        private bool m_Inited = false;
        private List<IProgressable> m_Progressables = new();
    }
}
