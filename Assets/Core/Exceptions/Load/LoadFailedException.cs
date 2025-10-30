using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class LoadFailedException : Exception
    {
        public LoadFailedException()
            : base() { }

        public LoadFailedException(string message)
            : base(message) { }

        public LoadFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
