using System.Numerics;
using Core;
using UnityEngine;

namespace GameMain.RunTime
{
    public static class GamePlayMatEvents
    {
        public struct MatPassDoorEvent : IEvent { }

        public struct MatChangeCheckPointEvent : IEvent
        {
            public Transform CheckTransform;

            public MatChangeCheckPointEvent(Transform checkTransform)
            {
                CheckTransform = checkTransform;
            }
        }

        public struct MatReCheckPointEvent : IEvent
        {
            public Transform CheckTransform;

            public MatReCheckPointEvent(Transform checkTransform)
            {
                CheckTransform = checkTransform;
            }
        }

        public struct MatPlayerDeathEvent : IEvent { }
    }
}
