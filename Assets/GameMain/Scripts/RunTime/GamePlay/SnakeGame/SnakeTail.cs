using UnityEngine;

namespace GameMain.RunTime
{
    public class SnakeTail : MonoBehaviour
    {
        private void Awake()
        {
            if (m_SpriteRenderer == null)
            {
                SetupVisuals();
            }
            m_Collider = GetComponent<Collider2D>();
        }

        public void Initialize(Vector2 size)
        {
            SetupVisuals(size);
            if (m_Collider is BoxCollider2D box)
            {
                box.size = size;
                box.offset = Vector2.zero;
            }
        }

        private void SetupVisuals(Vector2 size = default)
        {
            if (size == default)
                size = Vector2.one;

            if (m_SpriteRenderer == null || m_SpriteRenderer.transform == transform)
            {
                Transform visualsTransform = transform.Find("Visuals");
                if (visualsTransform == null)
                {
                    var visualsGo = new GameObject("Visuals");
                    visualsTransform = visualsGo.transform;
                    visualsTransform.SetParent(transform);
                }

                var sr = visualsTransform.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = visualsTransform.gameObject.AddComponent<SpriteRenderer>();
                    if (m_SpriteRenderer != null)
                    {
                        sr.sprite = m_SpriteRenderer.sprite;
                        sr.color = m_SpriteRenderer.color;
                        sr.sortingLayerID = m_SpriteRenderer.sortingLayerID;
                        sr.sortingOrder = m_SpriteRenderer.sortingOrder;
                        if (!Application.isPlaying)
                            DestroyImmediate(m_SpriteRenderer);
                        else
                            m_SpriteRenderer.enabled = false;
                    }
                }
                m_SpriteRenderer = sr;
            }

            m_SpriteRenderer.transform.localPosition = Vector3.zero;
        }

        public void SetPermanent()
        {
            if (m_IsPermanent)
                return;

            m_IsPermanent = true;
            if (m_Collider != null)
            {
                m_Collider.enabled = false;
            }

            if (m_SpriteRenderer != null)
            {
                var color = m_SpriteRenderer.color;
                color.a = 0.08f;
                m_SpriteRenderer.color = color;
            }
        }

        [SerializeField]
        private SpriteRenderer m_SpriteRenderer;

        private Collider2D m_Collider;
        private bool m_IsPermanent;

        public int PrefabIndex;
        public TailType TailType;
    }
}
