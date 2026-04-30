using System.Collections.Generic;
using UnityEngine;

namespace GameMain.RunTime
{
    public class BeeLdtkControl : AutoLdtkEntity
    {
        [LDtkField("StartCheckPoint")]
        [SerializeField]
        private Transform m_StartPoint;

        public Transform currentPoint;

        private void Awake()
        {
            if (m_StartPoint != null)
            {
                currentPoint = m_StartPoint;
            }
        }
    }
}
