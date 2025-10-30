using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class ProgressBarLoadInfo : ILoadInfo
    {
        public LoaderType GetNeededLoaderType()
        {
            return LoaderType.ProgressBar;
        }
    }
}
