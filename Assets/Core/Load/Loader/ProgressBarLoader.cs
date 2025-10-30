using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public class ProgressBarLoader : LoaderBase<ProgressBarLoadInfo>
    {
        public override LoaderType GetLoaderType()
        {
            return LoaderType.ProgressBar;
        }

        public override void InitLoader(ProgressBarLoadInfo loadInfo)
        {
            m_VgLoadProgressManager = VgLoadProgressManager.Instance;
        }

        public override UniTask InitLoadedThings()
        {
            m_VgLoadProgressManager.Init();
            return UniTask.CompletedTask;
        }

        private VgLoadProgressManager m_VgLoadProgressManager;
    }
}
