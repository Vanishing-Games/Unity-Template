using Core;
using UnityEngine;
using UnityEngine.Pool;

namespace GameMain.RunTime
{
    /// <summary>
    /// 挂载于特效预制体上，负责粒子播放逻辑与自动回收
    /// </summary>
    public class ParticleEffectInstance : MonoBehaviour
    {
        private ParticleSystem[] _childParticles;
        private IObjectPool<GameObject> _myPool;
        private bool _isInitialized = false;

        private float _timer;
        public float _maxDuration;

        void Awake()
        {
            // 预先获取所有子层级的粒子系统，减少运行开销
            _childParticles = GetComponentsInChildren<ParticleSystem>();
        }

        /// <summary>
        /// 初始化特效并开始播放
        /// </summary>
        /// <param name="pool">所属的对象池引用</param>
        /// <param name="maxLifeTime">强制回收的最大持续时间</param>
        public void Init(IObjectPool<GameObject> pool, float maxLifeTime)
        {
            _myPool = pool;
            _maxDuration = maxLifeTime;
            _timer = 0f;
            _isInitialized = true;

            // 停止上一次可能的残留播放，并清除现有粒子
            foreach (var ps in _childParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        void Update()
        {
            if (!_isInitialized)
                return;

            _timer += Time.deltaTime;

            // 判定条件：1. 超过预设最大时间  2. 所有粒子系统均已停止播放
            if (_timer >= _maxDuration || !IsAnyParticleAlive())
            {
                FinishAndReturn();
            }
        }

        private bool IsAnyParticleAlive()
        {
            // 检查所有子粒子系统是否还有存活的粒子
            foreach (var ps in _childParticles)
            {
                if (ps.IsAlive(true))
                    return true;
            }
            return false;
        }

        private void FinishAndReturn()
        {
            _isInitialized = false;

            // 强制停止排放，确保回收后在池中是静止的
            foreach (var ps in _childParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // 回收到池中
            _myPool?.Release(this.gameObject);
        }
    }
}
