using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;

namespace Core.ConsoleUtilities
{
    public partial class ConsoleCommands : MonoBehaviour
    {
        [ConsoleMethod("resload", "load a addressable resource and spawn it at given position")]
        public static void LoadAndPlace(string address, float x, float y, float z)
        {
            LoadAddressableCommand<GameObject> command = new(address);
            InstantiateGoCommand goCommand = new(command.Execute(), new Vector3(x, y, z));

            goCommand.Execute();
        }

        [ConsoleMethod("addressable_info", "print addressable system info")]
        public static void PrintAddressableInfo()
        {
            var command = new PrintAddressableInfoCommand();
            command.Execute();
        }
    }
}
