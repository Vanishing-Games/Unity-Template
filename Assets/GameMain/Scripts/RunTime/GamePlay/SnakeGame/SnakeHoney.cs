using System;
using Core;
using Newtonsoft.Json.Linq;
using R3;
using UnityEngine;

namespace GameMain.RunTime
{
    public enum HoneyState
    {
        Uncollected,
        CollectedTemporary,
        CollectedPermanent,
    }

    [Serializable]
    public class SnakeHoneySaveData
    {
        public HoneyState State;
    }

    public class SnakeHoney : AutoLdtkEntity
    {
        private void Awake()
        {
            MessageBroker
                .Global.Receive<GamePlaySnakeGameEvents.SnakeDeathEvent>()
                .Subscribe(_ => OnSnakeDeath())
                .AddTo(ref m_Disposables);

            MessageBroker
                .Global.Receive<GamePlaySnakeGameEvents.SnakeSaveEvent>()
                .Subscribe(_ => OnSnakeSave())
                .AddTo(ref m_Disposables);

            m_StableId = BuildStableId();
            LevelStateRegistry.Instance.Register(m_StableId, Capture, Restore);
        }

        private void OnDestroy()
        {
            LevelStateRegistry.Instance.Unregister(m_StableId);
            m_Disposables.Dispose();
        }

        private string BuildStableId()
        {
            string worldId = World != null ? World.Identifier : "World";
            string levelId = Level != null ? Level.Identifier : "Level";
            return $"Honey_{worldId}_{levelId}_{LdtkIid}";
        }

        private object Capture()
        {
            return new SnakeHoneySaveData { State = m_State };
        }

        private void Restore(JToken token)
        {
            var data = token?.ToObject<SnakeHoneySaveData>();
            if (data == null)
                return;
            if (data.State == HoneyState.CollectedPermanent)
            {
                m_State = HoneyState.CollectedPermanent;
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (m_State != HoneyState.Uncollected)
                return;

            if (other.GetComponentInParent<PlayerSnake>() != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            m_State = HoneyState.CollectedTemporary;
            gameObject.SetActive(false);
            MessageBroker.Global.Publish(
                new GamePlaySnakeGameEvents.HoneyCollectedEvent { Honey = this }
            );
        }

        private void OnSnakeDeath()
        {
            if (m_State == HoneyState.CollectedTemporary)
            {
                m_State = HoneyState.Uncollected;
                gameObject.SetActive(true);
                MessageBroker.Global.Publish(
                    new GamePlaySnakeGameEvents.HoneyResetEvent { Honey = this }
                );
            }
        }

        private void OnSnakeSave()
        {
            if (m_State == HoneyState.CollectedTemporary)
            {
                m_State = HoneyState.CollectedPermanent;
            }
        }

        public HoneyState State => m_State;
        private HoneyState m_State = HoneyState.Uncollected;
        private DisposableBag m_Disposables = new();
        private string m_StableId;
    }
}
