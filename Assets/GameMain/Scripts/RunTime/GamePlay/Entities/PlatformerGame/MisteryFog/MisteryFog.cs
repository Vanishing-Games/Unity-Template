using System;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameMain.RunTime
{
    [Serializable]
    public class MisteryFogSaveData
    {
        public bool IsRevealed;
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class MisteryFog : LDtkTriggerEntity
    {
        [Tooltip("material_rainrust_light")]
        [SerializeField]
        private Material m_Material;

        [SerializeField]
        private float m_FadeDuration = 1f;

        private SpriteRenderer m_SpriteRenderer;
        private bool m_IsRevealed;
        private string m_StableId;

        protected override void Awake()
        {
            base.Awake();

            m_SpriteRenderer = GetComponent<SpriteRenderer>();

            if (m_Material != null)
                m_SpriteRenderer.material = m_Material;

            m_SpriteRenderer.sortingLayerName = "Effect";

            string worldId = World != null ? World.Identifier : "World";
            string levelId = Level != null ? Level.Identifier : "Level";
            m_StableId = $"MisteryFog_{worldId}_{levelId}_{LdtkIid}";

            LevelStateRegistry.Instance.Register(m_StableId, Capture, Restore);
        }

        private void OnDestroy()
        {
            LevelStateRegistry.Instance.Unregister(m_StableId);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (m_IsRevealed)
                return;

            if (other.CompareTag("Player"))
                Reveal();
        }

        private void Reveal()
        {
            m_IsRevealed = true;
            m_SpriteRenderer
                .DOFade(0f, m_FadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private object Capture() => new MisteryFogSaveData { IsRevealed = m_IsRevealed };

        private void Restore(JToken token)
        {
            var data = token?.ToObject<MisteryFogSaveData>();
            if (data?.IsRevealed == true)
            {
                m_IsRevealed = true;
                gameObject.SetActive(false);
            }
        }
    }
}
