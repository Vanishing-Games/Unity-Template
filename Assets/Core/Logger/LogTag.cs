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
        public static readonly LogTag Audio = new("Audio");
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
        public static readonly LogTag Rendering = new("Rendering");
        public static readonly LogTag Game = new("Game");
        public static readonly LogTag Fuck = new("Fuck");
        public static readonly LogTag UI = new("UI");

        // ========== Second Tags ==========
        public static readonly LogTag AudioEntry = new("AudioEntry", Audio.Path);
        public static readonly LogTag CodeUnfucker = new("CodeUnfucker", Editor.Path);
        public static readonly LogTag SceneLoader = new("SceneLoader", Loading.Path);
        public static readonly LogTag GameCoreStart = new("GameCoreStart", GameCore.Path);
        public static readonly LogTag GameCoreDestroy = new("GameCoreDestroy", GameCore.Path);
        public static readonly LogTag GameQuit = new("GameQuit", GameCore.Path);
        public static readonly LogTag VgLoadingSplashManager = new(
            "VgLoadingSplashManager",
            Loading.Path
        );
        public static readonly LogTag VgCameraManager = new("VgCameraManager", CoreModule.Path);
        public static readonly LogTag PlayerControl = new("PlayerControl", Game.Path);
        public static readonly LogTag LdtkProcessor = new("LDtkProcessor", Editor.Path);
        public static readonly LogTag PlayerManager = new("PlayerManager", Game.Path);
        public static readonly LogTag LevelManager = new("LevelManager", Game.Path);
        public static readonly LogTag WavePoolManager = new("WavePoolManager", Game.Path);
        public static readonly LogTag Button = new("Button", UI.Path);

        // ========== Third Tags ==========
        public static readonly LogTag CodeUnfucker_3_Sample = new("Save", CodeUnfucker.Path); // MAX depth = 3
        public static readonly LogTag GameRunCheck = new("GameSystem", GameCoreStart.Path);
        public static readonly LogTag LdtkRoomProcessor = new("RoomProcessor", LdtkProcessor.Path);
        public static readonly LogTag LdtkLogicMapProcessor = new(
            "LogicMapProcessor",
            LdtkProcessor.Path
        );
        public static readonly LogTag LDtkTransitionProcessor = new(
            "LevelTransitionProcessor",
            LdtkProcessor.Path
        );
        public static readonly LogTag LDtkVolumGenProcessor = new(
            "LDtkVolumGenProcessor",
            LdtkProcessor.Path
        );
        public static readonly LogTag LDtkIdentifiersProcessor = new(
            "LDtkIdentifiersProcessor",
            LdtkProcessor.Path
        );

        public static readonly LogTag LevelRoom = new("LevelRoom", LevelManager.Path);
        public static readonly LogTag LDtkChapterBackgroundProcessor = new(
            "LDtkChapterBackgroundProcessor",
            LdtkProcessor.Path
        );
        public static readonly LogTag LDtkMaterialSetProcessor = new(
            "LDtkMaterialSetProcessor",
            LdtkProcessor.Path
        );
        public static readonly LogTag LDtkLevelRoomProcessor = new(
            "LDtkLevelRoomProcessor",
            LdtkProcessor.Path
        );
        public static readonly LogTag LDtkAutoEntityProcessor = new(
            "AutoEntityProcessor",
            LdtkProcessor.Path
        );
    }
}
