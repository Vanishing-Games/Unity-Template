using System.Collections.Generic;

namespace Core
{
    public sealed class LogTag
    {
        public string Name { get; }
        public List<string> Path { get; }

        private LogTag(string name, List<string> parentPath = null)
        {
            Name = name;
            Path = new List<string>(parentPath ?? new List<string>()) { name };

            if (Path.Count > 3)
            {
                throw new System.Exception(
                    $"Tag hierarchy too deep: {string.Join(" > ", Path)} (max 3 levels)"
                );
            }
        }

        public override string ToString() => $"[{string.Join("][", Path)}]";

        // ========== Base Tags ==========
        public static readonly LogTag GameCore = new("GameCore");
        public static readonly LogTag Editor = new("Editor");
        public static readonly LogTag Loading = new("Loading");
        public static readonly LogTag Event = new("Event");
        public static readonly LogTag CoreModule = new("CoreModule");
        public static readonly LogTag Input = new("Input");
        public static readonly LogTag Math = new("Math");
        public static readonly LogTag Addressables = new("Addressables");
        public static readonly LogTag Test = new("Test");
        public static readonly LogTag Command = new("Command");

        // ========== Second Tags ==========
        public static readonly LogTag CodeUnfucker = new("CodeUnfucker", Editor.Path);
        public static readonly LogTag SceneLoader = new("SceneLoader", Loading.Path);
        public static readonly LogTag GameCoreStart = new("GameCoreStart", GameCore.Path);
        public static readonly LogTag GameCoreDestroy = new("GameCoreDestroy", GameCore.Path);
        public static readonly LogTag GameQuit = new("GameQuit", GameCore.Path);
        public static readonly LogTag VgLoadProgressManager = new(
            "VgLoadProgressManager",
            Loading.Path
        );

        // ========== Third Tags ==========
        public static readonly LogTag CodeUnfucker_3_Sample = new("Save", CodeUnfucker.Path); // MAX depth = 3
        public static readonly LogTag GameRunCheck = new("GameSystem", GameCoreStart.Path);
    }
}
