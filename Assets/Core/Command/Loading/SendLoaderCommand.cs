using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class SendLoaderCommand : AbastractManagerCommand<LoadManager>
    {
        private ILoader m_Loader;

        public SendLoaderCommand(ILoader loader)
        {
            m_Loader = loader;
        }

        public override bool Execute()
        {
            if (m_Manager == null)
            {
                Logger.LogError(
                    "[SendLoaderCommand] SceneLoadManager is not initialized, making excution for SendLoaderCommand failed"
                );
                return false;
            }

            m_Manager.RegisterLoader(m_Loader);
            return true;
        }
    }
}
