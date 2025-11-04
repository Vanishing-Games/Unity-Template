using UnityEngine;

namespace Core
{
    public static class Vector2Extension
    {
        public static Vector2 ProjectOnLine(this Vector2 v, Vector2 lineNormal)
        {
            if (lineNormal.sqrMagnitude <= Mathf.Epsilon)
            {
#if UNITY_EDITOR
                Logger.LogWarn("ProjectOnLine: lineNormal is zero. Returning original vector.",LogTag.Math);
#endif
                return v;
            }

            Vector2 n = lineNormal.normalized;
            return v - Vector2.Dot(v, n) * n;
        }
    }
}
