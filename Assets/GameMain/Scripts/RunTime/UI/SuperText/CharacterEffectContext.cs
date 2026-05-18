using System.Collections.Generic;
using UnityEngine;

namespace GameMain.RunTime
{
    public struct CharacterEffectContext
    {
        public Vector3[] Vertices;
        public Color32[] Colors;
        public int VertexIndex;
        public int VisibleCharIndex;
        public float TotalTime;
        public IReadOnlyDictionary<string, string> Attributes;
    }
}
