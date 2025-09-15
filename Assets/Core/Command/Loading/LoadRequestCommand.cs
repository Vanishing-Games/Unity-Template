using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;

namespace Core
{
    public class LoadRequestCommand : AbastractManagerCommand<LoadManager>
    {
        public LoadRequestEvent LoadEvent { get; set; }

        public LoadRequestCommand(LoadRequestEvent loadEvent)
        {
            LoadEvent = loadEvent;
        }

        public override bool Execute()
        {
            if (m_Manager == null)
            {
                Logger.DebugLogError(
                    "[LoadCommand] LoadManager is not initialized, making excution for LoadEvent failed"
                );

                return false;
            }
            
            m_Manager.PrepareForLoad(LoadEvent);
            MessageBroker.Global.PublishComplete(LoadEvent);

            return true;
        }
    }
}
