using Core;
using Cysharp.Threading.Tasks;

namespace GameMain.RunTime
{
    public static class GameFlowCommands
    {
        public class BackToMenuCommand : IGameFlowCommand
        {
            public UniTask Execute()
            {
                GameCore.Instance.RequestExitToMenu();
                return UniTask.CompletedTask;
            }

            public string CommandName => "game:menu";
        }

        public class StartGameCommand : IGameFlowCommand
        {
            public StartGameCommand(string savePointName)
            {
                m_SavePointName = savePointName;
            }

            public UniTask Execute()
            {
                if (!string.IsNullOrEmpty(m_SavePointName))
                {
                    GameCore.Instance.RequestLoadLevelFromSavePoint(m_SavePointName);
                }
                else
                {
                    GameCore.Instance.RequestLoadLevel(m_ChapterId, m_LevelId);
                }
                return UniTask.CompletedTask;
            }

            public string CommandName =>
                !string.IsNullOrEmpty(m_SavePointName)
                    ? $"game:start savepoint:{m_SavePointName}"
                    : $"game:start {m_ChapterId}/{m_LevelId}:{m_SpawnPointIndex}";

            private readonly string m_ChapterId;
            private readonly string m_LevelId;
            private readonly int m_SpawnPointIndex;
            private readonly string m_SavePointName;
        }
    }
}
