using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Newtonsoft.Json.Linq;
using R3;
using UnityEngine;

namespace GameMain.RunTime
{
    [Serializable]
    public class SnakeDoorSaveData
    {
        public bool IsPermanentlyOpen;
    }

    public class SnakeDoor : AutoLdtkEntity
    {
        public override void OnPostImport()
        {
            CheckConditions();
        }

        private void Awake()
        {
            m_BoxCollider = GetComponent<BoxCollider2D>();

            MessageBroker
                .Global.Receive<GamePlaySnakeGameEvents.HoneyCollectedEvent>()
                .Subscribe(_ => CheckConditions())
                .AddTo(ref m_Disposables);

            MessageBroker
                .Global.Receive<GamePlaySnakeGameEvents.HoneyResetEvent>()
                .Subscribe(_ => CheckConditions())
                .AddTo(ref m_Disposables);

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

        private void Start()
        {
            CheckConditions();
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
            return $"Door_{worldId}_{levelId}_{LdtkIid}";
        }

        private object Capture()
        {
            return new SnakeDoorSaveData { IsPermanentlyOpen = m_IsPermanentlyOpen };
        }

        private void Restore(JToken token)
        {
            var data = token?.ToObject<SnakeDoorSaveData>();
            if (data == null)
                return;
            if (data.IsPermanentlyOpen)
            {
                m_IsPermanentlyOpen = true;
                SetDoorOpen(true);
            }
        }

        private void CheckConditions()
        {
            if (m_IsPermanentlyOpen)
                return;

            bool allCollected =
                m_AssociatedHoneys.Count > 0
                && m_AssociatedHoneys.All(h => h.State != HoneyState.Uncollected);
            m_Animator.SetFloat("KeyNum", m_AssociatedHoneys.Count);
            m_Animator.SetFloat(
                "KeyHas",
                m_AssociatedHoneys.Count(h => h.State != HoneyState.Uncollected)
            );
            SetDoorOpen(allCollected);
        }

        private void SetDoorOpen(bool isOpen)
        {
            //gameObject.SetActive(!isOpen);
            if (m_BoxCollider != null)
                m_BoxCollider.enabled = !isOpen;
        }

        private void OnSnakeDeath()
        {
            if (!m_IsPermanentlyOpen)
                CheckConditions();
        }

        private void OnSnakeSave()
        {
            if (!gameObject.activeSelf)
                m_IsPermanentlyOpen = true;
        }

        [LDtkField("AssociatedHoneys")]
        [SerializeField]
        private List<SnakeHoney> m_AssociatedHoneys = new();

        [SerializeField]
        private Animator m_Animator;

        private BoxCollider2D m_BoxCollider;

        private DisposableBag m_Disposables = new();
        private bool m_IsPermanentlyOpen;
        private string m_StableId;
    }
}
