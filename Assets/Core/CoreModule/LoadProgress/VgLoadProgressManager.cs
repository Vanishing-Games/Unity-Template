using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class VgLoadProgressManager : MonoSingletonLasy<VgLoadProgressManager>
    {
        public void Show() { }

        public void UpdateProgress(float progress) { }

        public void Hide() { }
    }
}
