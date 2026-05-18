using System;
using System.Collections.Generic;

namespace GameMain.RunTime
{
    public static class CharacterEffectRegistry
    {
        public static void Register(string tagName, Func<ICharacterEffect> factory)
        {
            s_Factories[tagName] = factory;
        }

        public static bool IsRegistered(string tagName)
        {
            return s_Factories.ContainsKey(tagName);
        }

        public static ICharacterEffect Create(string tagName)
        {
            return s_Factories.TryGetValue(tagName, out var factory) ? factory() : null;
        }

        private static readonly Dictionary<string, Func<ICharacterEffect>> s_Factories = new()
        {
            ["shake"] = () => new ShakeEffect(),
        };
    }
}
