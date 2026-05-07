using System.Collections.Generic;
using System.Diagnostics;
using Core;
using UnityEngine;
using UnityEngine.Pool;

namespace GameMain.RunTime
{
    /// <summary>
    /// 全局特效管理器：自动加载资源并管理对象池
    /// </summary>
    public class EffectPoolManager : MonoSingletonPersistent<EffectPoolManager>
    {
        [Header("配置")]
        [Tooltip("存放特效预制体的路径 (相对于 Resources 文件夹)")]
        public string effectPath = "Prefabs/Effect";

        public int defaultPoolSize = 2;
        public int maxPoolSize = 20;

        // 存储池子的字典，Key 为预制体名称
        private Dictionary<string, IObjectPool<GameObject>> _poolDictionary =
            new Dictionary<string, IObjectPool<GameObject>>();

        protected override void Awake()
        {
            base.Awake();
            InitializePools();
        }

        private void InitializePools()
        {
            // 1. 从 Resources 文件夹加载所有 GameObject
            GameObject[] prefabs = Resources.LoadAll<GameObject>(effectPath);

            if (prefabs.Length == 0)
            {
                UnityEngine.Debug.LogWarning(
                    $"EffectPoolManager: 在 Resources/{effectPath} 路径下未找到任何预制体。"
                );
                return;
            }

            foreach (var prefab in prefabs)
            {
                GameObject currentPrefab = prefab;
                string effectName = currentPrefab.name;

                // 2. 为每个预制体配置对象池逻辑
                var pool = new ObjectPool<GameObject>(
                    createFunc: () =>
                    {
                        GameObject obj = Instantiate(currentPrefab, transform);
                        // 动态添加逻辑控制组件（如果预制体没挂的话）
                        if (!obj.TryGetComponent<ParticleEffectInstance>(out var instance))
                        {
                            obj.AddComponent<ParticleEffectInstance>();
                        }
                        return obj;
                    },
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    collectionCheck: false,
                    defaultCapacity: defaultPoolSize,
                    maxSize: maxPoolSize
                );

                _poolDictionary.Add(effectName, pool);
                UnityEngine.Debug.Log($"[EffectPool] 已初始化池: {effectName}");
            }
        }

        /// <summary>
        /// 生成特效
        /// </summary>
        /// <param name="effectName">预制体文件名</param>
        /// <param name="position">生成位置</param>
        /// <param name="maxDuration">强制回收时间（默认 5 秒）</param>
        public void SpawnEffect(string effectName, Vector3 position, float maxDuration = 5f)
        {
            if (_poolDictionary.TryGetValue(effectName, out var pool))
            {
                GameObject effectObj = pool.Get();
                effectObj.transform.position = position;

                // 初始化粒子播放与生命周期
                var instance = effectObj.GetComponent<ParticleEffectInstance>();
                instance.Init(pool, maxDuration);
            }
            else
            {
                UnityEngine.Debug.LogError(
                    $"[EffectPool] 找不到名为 '{effectName}' 的特效池。请检查文件名和 Resources 路径。"
                );
            }
        }
    }
}
