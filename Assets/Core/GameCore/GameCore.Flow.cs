using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using R3;
using UnityHFSM;

namespace Core
{
    public partial class GameCore : MonoSingletonPersistent<GameCore>
    {
        public void RegisterSystem(ICoreModuleSystem system)
        {
            m_Systems ??= new CoreSystemRegistry();
            m_Systems.Register(system);
        }

        public void RequestLoadLevel(string savePointName)
        {
            m_LoadContext = LoadContext.ForSavePoint(savePointName);
            m_Fsm.Trigger(
                m_Fsm.ActiveStateName == GameFlowState.InLevel
                    ? GameFlowTrigger.SwitchLevel
                    : GameFlowTrigger.StartGame
            );
        }

        public void RequestLoadLevel(string chapterId, string levelId)
        {
            m_LoadContext = LoadContext.ForLevel(chapterId, levelId);
            m_Fsm.Trigger(
                m_Fsm.ActiveStateName == GameFlowState.InLevel
                    ? GameFlowTrigger.SwitchLevel
                    : GameFlowTrigger.StartGame
            );
        }

        public void RequestLoadLevelFromSavePoint(string savePointName)
        {
            m_LoadContext = LoadContext.ForSavePoint(savePointName);
            m_Fsm.Trigger(
                m_Fsm.ActiveStateName == GameFlowState.InLevel
                    ? GameFlowTrigger.SwitchLevel
                    : GameFlowTrigger.StartGame
            );
        }

        public void RequestExitToMenu()
        {
            m_LoadContext = LoadContext.ForMainMenu();
            m_Fsm.Trigger(GameFlowTrigger.ExitToMenu);
        }

        internal void OnFlowUpdate() => m_Fsm.OnLogic();

        private async Task InitFlow()
        {
#if UNITY_EDITOR
            if (m_GameCoreMode == GameCoreMode.EditorFast)
            {
                CLogger.LogInfo(
                    "[GameCore] Editor Fast Mode Enabled: Skipping Booting and MainMenu, going directly to Loading state.",
                    LogTag.GameCoreStart
                );
                m_Fsm = new StateMachine<string, GameFlowState, GameFlowTrigger>();
                m_Fsm.AddState(
                    GameFlowState.Loading,
                    onEnter: async _ =>
                    {
                        await RunBooting();
                        await RunLoading();
                    }
                );
                m_Fsm.AddState(
                    GameFlowState.InLevel,
                    onEnter: _ => SubscribeInLevelEvents(),
                    onExit: _ => UnsubscribeInLevelEvents()
                );
                m_Fsm.AddTriggerTransition(
                    GameFlowTrigger.LoadComplete,
                    GameFlowState.Loading,
                    GameFlowState.InLevel
                );
                m_Fsm.SetStartState(GameFlowState.Loading);
                m_Fsm.Init();
                return;
            }
#endif

            m_Systems ??= new CoreSystemRegistry();
            m_Systems.OnBootStart(new ShowLogoStep().Execute, order: 10);
            m_Systems.OnBootStart(new LoadMainMenuSceneStep().Execute, order: 100);

            InitFsm();

            m_Fsm.SetStartState(GameFlowState.Booting);
            m_Fsm.Init();
        }

        private void InitFsm()
        {
            m_Fsm = new StateMachine<string, GameFlowState, GameFlowTrigger>();

            m_Fsm.AddState(GameFlowState.Booting, onEnter: _ => RunBooting().Forget());
            m_Fsm.AddState(
                GameFlowState.MainMenu,
                onEnter: _ =>
                {
                    CLogger.LogInfo("[GameCore] → MainMenu", LogTag.GameCoreStart);
                    m_Systems.FireOnMainMenuEnter().Forget();
                },
                onExit: _ =>
                {
                    CLogger.LogInfo("[GameCore] ← MainMenu", LogTag.GameCoreStart);
                    m_Systems.FireOnMainMenuExit().Forget();
                }
            );
            m_Fsm.AddState(GameFlowState.Loading, onEnter: _ => RunLoading().Forget());
            m_Fsm.AddState(
                GameFlowState.InLevel,
                onEnter: _ => SubscribeInLevelEvents(),
                onExit: _ => UnsubscribeInLevelEvents()
            );

            m_Fsm.AddTriggerTransition(
                GameFlowTrigger.BootComplete,
                GameFlowState.Booting,
                GameFlowState.MainMenu
            );
            m_Fsm.AddTriggerTransition(
                GameFlowTrigger.StartGame,
                GameFlowState.MainMenu,
                GameFlowState.Loading
            );
            m_Fsm.AddTriggerTransition(
                GameFlowTrigger.ExitToMenu,
                GameFlowState.InLevel,
                GameFlowState.Loading
            );
            m_Fsm.AddTriggerTransition(
                GameFlowTrigger.SwitchLevel,
                GameFlowState.InLevel,
                GameFlowState.Loading
            );
            m_Fsm.AddTriggerTransition(
                GameFlowTrigger.LoadComplete,
                GameFlowState.Loading,
                GameFlowState.InLevel,
                condition: _ => m_LoadContext.Destination == GameFlowState.InLevel
            );
            m_Fsm.AddTriggerTransition(
                GameFlowTrigger.LoadComplete,
                GameFlowState.Loading,
                GameFlowState.MainMenu,
                condition: _ => m_LoadContext.Destination == GameFlowState.MainMenu
            );
        }

        private async UniTask RunBooting()
        {
            CLogger.LogInfo("[GameCore] Booting...", LogTag.GameCoreStart);

            await m_Systems.InitializeAllAsync();
            await m_Systems.FireOnBootStart();

            m_Fsm.Trigger(GameFlowTrigger.BootComplete);
        }

        private async UniTask RunLoading()
        {
            CLogger.LogInfo("[GameCore] → Loading", LogTag.GameCoreStart);

            await m_Systems.FireOnLoadStart(m_LoadContext);
            await m_Systems.FireOnLoadScene(m_LoadContext);
            await m_Systems.FireOnLoadComplete(m_LoadContext);

            m_Fsm.Trigger(GameFlowTrigger.LoadComplete);
        }

        private void SubscribeInLevelEvents()
        {
            CLogger.LogInfo("[GameCore] → InLevel", LogTag.GameCoreStart);
            m_InLevelSubs = new DisposableBag();
            MessageBroker
                .Global.Subscribe<GameCoreEvents.LevelClearEvent>(OnLevelClear)
                .AddTo(ref m_InLevelSubs);

            m_Systems.FireOnInLevelEnter(m_LoadContext).Forget();
        }

        private void UnsubscribeInLevelEvents()
        {
            CLogger.LogInfo("[GameCore] ← InLevel", LogTag.GameCoreStart);
            m_InLevelSubs.Dispose();
            m_Systems.FireOnInLevelExit().Forget();
        }

        private void OnLevelClear(GameCoreEvents.LevelClearEvent e)
        {
            CLogger.LogInfo($"Level Clear! Next: {e.NextSavePointName}", LogTag.GameCoreStart);
            RequestLoadLevel(e.NextSavePointName);
        }

        public GameFlowState CurrentState => m_Fsm.ActiveStateName;
        public bool IsBootedFromEntry { get; private set; }

        private StateMachine<string, GameFlowState, GameFlowTrigger> m_Fsm;
        private CoreSystemRegistry m_Systems;
        private LoadContext m_LoadContext;
        private DisposableBag m_InLevelSubs;
    }
}
