using Core;

namespace GameMain.RunTime
{
    public static class SuperTextEvents
    {
        public struct TextRevealCompletedEvent : IEvent { }

        public struct CharacterRevealedEvent : IEvent
        {
            public readonly int CharIndex;

            public CharacterRevealedEvent(int charIndex)
            {
                CharIndex = charIndex;
            }
        }
    }
}
