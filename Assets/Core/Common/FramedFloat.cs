using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core
{
    [InlineProperty]
    public struct FramedFloat
    {
        [HorizontalGroup("Row"), LabelText("Frames"), OdinSerialize]
        public int Frames
        {
            get => frames;
            set
            {
                frames = Mathf.Max(0, value);
                seconds = frames / BASE_FPS;
            }
        }

        [HorizontalGroup("Row"), LabelText("Seconds"), OdinSerialize]
        public float Seconds
        {
            get => seconds;
            set
            {
                seconds = Mathf.Max(0, value);
                frames = Mathf.RoundToInt(seconds * BASE_FPS);
            }
        }

        public static implicit operator float(FramedFloat f)
        {
            return f.seconds;
        }

        public static implicit operator FramedFloat(float value)
        {
            return new FramedFloat { Seconds = value };
        }

        public const float BASE_FPS = 60f;
        private float seconds;
        private int frames;
    }
}
