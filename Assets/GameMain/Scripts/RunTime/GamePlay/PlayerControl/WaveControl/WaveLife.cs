using UnityEngine;
using UnityEngine.Pool;

namespace GameMain.RunTime
{
    public class WaveLife : MonoBehaviour
    {
        [Header("扩散配置")]
        public float maxRadius = 5f;
        public float duration = 1.5f;

        [Header("动画曲线")]
        public AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Header("发射源信息")]
        public int sourceID; // 由发射者注入的 ID

        private SpriteRenderer spriteRenderer;
        private float timer = 0f;
        private IObjectPool<WaveLife> _pool; // 对所属对象池的引用

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 初始化方法：由池管理器或发射者调用
        public void Init(int id, IObjectPool<WaveLife> pool, float radius, float timeDur)
        {
            this.sourceID = id;
            this._pool = pool;
            this.maxRadius = radius; // 动态设置最大半径
            this.duration = timeDur; // 动态设置持续时间

            // 重置动画状态
            timer = 0f;
            transform.localScale = Vector3.zero;
            SetAlpha(fadeCurve.Evaluate(0f));
        }

        void Update()
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // 1. 缩放动画
            float currentScale = expansionCurve.Evaluate(progress) * maxRadius;
            transform.localScale = new Vector3(currentScale, currentScale, 1f);

            // 2. 透明度动画
            SetAlpha(fadeCurve.Evaluate(progress));

            // 3. 生命周期结束
            if (progress >= 1f)
            {
                ReturnToPool();
            }
        }

        private void SetAlpha(float alpha)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
        }

        private void ReturnToPool()
        {
            // 如果对象池还存在，则归还；否则销毁（防止切关报错）
            if (_pool != null)
                _pool.Release(this);
            else
                Destroy(gameObject);
        }
    }
}
