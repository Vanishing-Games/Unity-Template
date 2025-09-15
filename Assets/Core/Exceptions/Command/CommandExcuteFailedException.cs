using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class CommandExcuteFailedException : Exception
    {
        public CommandExcuteFailedException()
            : base() { }

        public CommandExcuteFailedException(string message)
            : base(message) { }

        public CommandExcuteFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
