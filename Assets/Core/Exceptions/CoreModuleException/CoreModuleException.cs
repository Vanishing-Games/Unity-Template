using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class CoreModuleException : Exception
    {
        public CoreModuleException() { }

        public CoreModuleException(string message)
            : base(message) { }

        public CoreModuleException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
