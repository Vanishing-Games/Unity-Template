using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameMain.RunTime
{
    [Serializable]
    public class PlayerSnakeSaveData
    {
        public Vector3 CheckPos;
        public List<SnakeTailRecord> Tails = new();
    }

    [Serializable]
    public struct SnakeTailRecord
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public TailType Type;
        public int PrefabIndex;
    }
}
