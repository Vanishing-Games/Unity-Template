using Core;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace GameMain.RunTime
{
    public class SavePoint : LDtkTriggerEntity
    {
        [LDtkField]
        public string PointName;

        [LDtkField]
        public string NickName;

        [Header("Visuals")]
        [SerializeField]
        protected SpriteRenderer m_SpriteRenderer;

        [SerializeField]
        protected Sprite m_UnsavedSprite;

        [SerializeField]
        protected Sprite m_SavedSprite;

        [SerializeField]
        private Animator m_Animator;

        [SerializeField]
        protected string m_SaveAnimationTrigger = "Save";

        private DisposableBag m_Disposables = new();

        [SerializeField]
        private bool m_IsSaved;

        protected override void Awake()
        {
            base.Awake();
            if (m_SpriteRenderer != null && m_UnsavedSprite != null)
            {
                m_SpriteRenderer.sprite = m_UnsavedSprite;
            }
        }

        private void OnEnable()
        {
            MessageBroker
                .Global.Subscribe<GamePlaySnakeGameEvents.SnakeSaveEvent>(_ => OnSave())
                .AddTo(ref m_Disposables);
        }

        private void OnDisable()
        {
            m_Disposables.Dispose();
        }

        private void OnSave()
        {
            if (m_IsSaved)
            {
                m_IsSaved = false;
                m_Animator.SetBool("IsSaved", m_IsSaved);
            }
        }

        public override void OnPostImport()
        {
            // 如果 LDtk 没有配置名字，则自动生成一个唯一的关卡内名字
            if (string.IsNullOrEmpty(PointName))
            {
                string worldId = World != null ? World.Identifier : "World";
                string levelId = Level != null ? Level.Identifier : "Level";
                PointName = $"{worldId}_{levelId}_{LdtkIid}";
            }

            if (m_SpriteRenderer != null && m_UnsavedSprite != null)
            {
                m_SpriteRenderer.sprite = m_UnsavedSprite;
            }
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (m_IsSaved)
                return;

            if (other.CompareTag("Player"))
            {
                OnSaveTriggered();
                m_IsSaved = true;
                PlaySaveVisuals();
            }
        }

        private void OnSaveTriggered()
        {
            CLogger.LogInfo($"Player reached save point: {PointName} ({NickName})", LogTag.Game);
            MessageBroker.Global.Publish(new GamePlaySnakeGameEvents.SnakeSaveEvent());
            MessageBroker.Global.Publish(
                new GamePlaySnakeGameEvents.SnakeCheckPointEvent(this.transform.position)
            );
            LevelManager.Instance.RecordLastSavePoint(PointName);
            VgSaveSystem.Instance.WriteSlotSaveAsync().Forget();
        }

        private void PlaySaveVisuals()
        {
            if (m_Animator != null)
            {
                m_Animator.SetBool("IsSaved", m_IsSaved);
            }
            else if (m_SpriteRenderer != null && m_SavedSprite != null)
            {
                m_SpriteRenderer.sprite = m_SavedSprite;
            }
        }
    }
}
