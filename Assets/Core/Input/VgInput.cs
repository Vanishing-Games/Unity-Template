using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Core
{
    public static class VgInput
    {
        public static float GetAxis(InputAxis axis)
        {
            return Input.GetAxis(axis.ToString());
        }

        public static bool PressedButton(InputAction action)
        {
            return Input.GetKeyDown(action.ToString());
        }

        public static bool HeldingButton(InputAction action)
        {
            return Input.GetKey(action.ToString());
        }

        public static bool ReleasedButton(InputAction action)
        {
            return Input.GetKeyUp(action.ToString());
        }
    }
}
