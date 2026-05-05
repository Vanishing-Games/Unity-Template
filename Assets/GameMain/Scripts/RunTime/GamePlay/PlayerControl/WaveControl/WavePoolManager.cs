using Core;
using UnityEngine;
using UnityEngine.Pool;

namespace GameMain.RunTime
{
    public class WavePoolManager : MonoSingletonPersistent<WavePoolManager>
    {
        [SerializeField]
        private WaveLife wavePrefab;

        [SerializeField]
        private int defaultSize = 10;

        [SerializeField]
        private int maxSize = 50;

        private IObjectPool<WaveLife> _pool;

        protected override void Awake()
        {
            base.Awake();
            wavePrefab = Resources.Load<WaveLife>("Prefabs/Wave/Wave");
        }

        void Start()
        {
            // 初始化 Unity 内置对象池
            _pool = new ObjectPool<WaveLife>(
                createFunc: () => Instantiate(wavePrefab, transform), // 创建新实例
                actionOnGet: (wave) => wave.gameObject.SetActive(true), // 从池中取出时
                actionOnRelease: (wave) => wave.gameObject.SetActive(false), // 归还池中时
                actionOnDestroy: (wave) => Destroy(wave.gameObject), // 池子过载销毁时
                collectionCheck: false,
                defaultCapacity: defaultSize,
                maxSize: maxSize
            );
        }

        // 提供给角色的调用接口
        public void SpawnWave(
            Vector3 position,
            int senderID,
            float radius = 5f,
            float duration = 1.5f
        )
        {
            WaveLife wave = _pool.Get();
            wave.transform.position = position;

            // 将变量传给 wave 实例
            wave.Init(senderID, _pool, radius, duration);
        }
    }
}
