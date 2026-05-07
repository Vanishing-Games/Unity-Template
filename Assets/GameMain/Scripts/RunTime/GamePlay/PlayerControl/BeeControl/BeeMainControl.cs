using Core;
using R3;
using Sirenix.Serialization;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public enum BeeState
    {
        StaySt,
        FollowSt,
        ThrowedSt,
    }

    public class BeeMainControl : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody2D>();
            m_BoxCollider = GetComponent<BoxCollider2D>();
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            m_BeeLdtkControl = GetComponent<BeeLdtkControl>();

            Random.InitState(this.gameObject.GetInstanceID());

            // 这样每个物体的随机序列都是独一无二的
            myUniqueVar = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 2f));
            CheckPointFollowOffset = myUniqueVar;
        }

        void Start()
        {
            // 将自身的父物体设为 null
            //transform.SetParent(null);
            //player = GameObject.FindWithTag("Player").transform;
            FollowCheckPoint = m_BeeLdtkControl.currentPoint;
            m_Transform = GetComponent<Transform>();
        }

        private void OnEnable()
        {
            MessageBroker
                .Global.Subscribe<GamePlayMatEvents.MatChangeCheckPointEvent>(CurrentPointSet)
                .AddTo(ref m_Disposables);
            MessageBroker
                .Global.Subscribe<GamePlayMatEvents.MatPlayerDeathEvent>(_ => OnPlayerRespawn())
                .AddTo(ref m_Disposables);
            MessageBroker
                .Global.Subscribe<GamePlayMatEvents.MatPlayerPassDoorEvent>(_ => OnPlayerRespawn())
                .AddTo(ref m_Disposables);
        }

        private void OnDisable()
        {
            m_Disposables.Dispose();
        }

        // Update is called once per frame
        void Update() { }

        private void FixedUpdate()
        {
            if (player == null)
            {
                //player = GameObject.FindWithTag("Player").transform;
                player = GameMain.TryGetPlayer().transform;
            }
            else
            {
                switch (currentState)
                {
                    case BeeState.StaySt:
                        StayStUpdate();
                        break;
                    case BeeState.ThrowedSt:
                        ThrowedStUpdate();
                        break;
                    case BeeState.FollowSt:
                        FollowStUpdate();
                        break;
                }
                currentSpeed = m_RigidBody.linearVelocity.magnitude;
            }
        }

        public void ChangeState(BeeState toState)
        {
            OnExitState(currentState);

            currentState = toState;

            OnEnterState(currentState);
        }

        void OnExitState(BeeState state)
        {
            switch (currentState)
            {
                case BeeState.StaySt:
                    StayStExit();
                    break;
                case BeeState.ThrowedSt:
                    ThrowedStExit();
                    break;
                case BeeState.FollowSt:
                    FollowStExit();
                    break;
            }
        }

        void OnEnterState(BeeState state)
        {
            switch (currentState)
            {
                case BeeState.StaySt:
                    StayStEnter();
                    break;
                case BeeState.ThrowedSt:
                    ThrowedStEnter();
                    break;
                case BeeState.FollowSt:
                    FollowStEnter();
                    break;
            }
        }

        //投出状态管理
        void ThrowedStEnter()
        {
            gameObject.layer = LayerMask.GetMask("Hook");
        }

        void ThrowedStUpdate() { }

        void ThrowedStExit() { }

        //跟随状态管理
        void FollowStEnter()
        {
            //m_BoxCollider.enabled = false;
            gameObject.layer = LayerMask.NameToLayer("WaveCheck");
        }

        void FollowStUpdate()
        {
            //改变虫子的朝向
            if (isFollowPlayer)
            {
                m_Transform.localScale = new Vector3(
                    Mathf.Sign(m_Transform.position.x - player.position.x),
                    1,
                    1
                );
            }

            FollowPointSet();

            //过远闪烁
            if (targetDistance() > FlashMoveDistance)
            {
                m_Transform.position = FollowPoint;
            }
            //近距离跟随
            else if (targetDistance() <= FlashMoveDistance)
            {
                MoveTowardsTarget(m_RigidBody, FollowPoint, FollowSpeedMult);
            }
            //围绕近距离跟随时的浮动值移动(先不考虑)
            else
            {
                m_RigidBody.linearVelocity = Vector2.zero;
            }
        }

        void FollowStExit()
        {
            m_BoxCollider.enabled = true;
            gameObject.layer = LayerMask.NameToLayer("Hook");
        }

        //悬挂状态管理
        void StayStEnter()
        {
            gameObject.layer = LayerMask.GetMask("Hook");
            StayToFollowCdTimer = 0;
        }

        void StayStUpdate()
        {
            if (StayToFollowCdTimer < StayToFollowCdTime)
                StayToFollowCdTimer++;
        }

        void StayStExit()
        {
            //gameObject.layer = LayerMask.GetMask("Bee");
        }

        //外部调用
        public void BeeThrow(Vector2 ThrowVelocity, bool isFaceRight)
        {
            m_SpriteRenderer.enabled = true;
            ChangeState(BeeState.ThrowedSt);
            m_RigidBody.linearVelocity = ThrowVelocity;
            FaceDirSet(isFaceRight);
        }

        public void FaceDirSet(bool isFaceRight)
        {
            if (isFaceRight)
                transform.localScale = new Vector3(-1, 1, 1);
            else
                transform.localScale = new Vector3(1, 1, 1);
        }

        public void FlashToPosition(Vector3 positon, bool isHidden)
        {
            //这里在原地留下闪烁特效

            m_Transform.position = positon;
            if (isHidden)
                m_SpriteRenderer.enabled = false;
            else
                m_SpriteRenderer.enabled = true;
        }

        //其他函数
        void FollowPointSet()
        {
            if (isFollowPlayer)
            {
                bool isRight = m_Transform.position.x > player.position.x;
                //超出范围时生成一个新点位
                if (Vector3.Distance(FollowPoint, player.position) > FollowPointDistance)
                {
                    FollowPoint =
                        GetSidePos(player.position, FollowPointDistance, isRight, 30f)
                        + CheckPointFollowOffset;
                }
            }
            else
            {
                FollowPoint =
                    FollowCheckPoint.position + CheckPointFollowOffset + new Vector3(0, 1, 0);
            }
        }

        void MoveTowardsTarget(Rigidbody2D rb, Vector2 targetPos, float speedMultiplier)
        {
            Vector2 offset = targetPos - (Vector2)m_Transform.position;
            float distance = offset.magnitude;
            rb.linearVelocity = offset.normalized * distance * speedMultiplier;
        }

        void CurrentPointSet(GamePlayMatEvents.MatChangeCheckPointEvent e)
        {
            if (isFollowPlayer)
            {
                m_BeeLdtkControl.currentPoint = e.CheckTransform;
                FollowCheckPoint = m_BeeLdtkControl.currentPoint;
                if (currentState == BeeState.StaySt)
                {
                    ChangeState(BeeState.FollowSt);
                }
            }
        }

        void OnPlayerRespawn()
        {
            if (isFollowPlayer)
            {
                CLogger.LogInfo("Bee回去了");
                isFollowPlayer = false;
                if (currentState != BeeState.FollowSt)
                {
                    ChangeState(BeeState.FollowSt);
                }
                MessageBroker.Global.Publish(new BeeManagerEvents.BeeRemoveEvents(this.gameObject));
            }
        }

        //噪声点位生成
        public Vector3 GetNoisePosition(Vector3 basePos, float seed, float freq, float amp)
        {
            // 使用 Time.time 使得噪声随时间平滑推移
            float time = Time.time * freq;

            // 为 X 和 Y 使用不同的采样坐标（seed 确保了个体差异，Offset 确保了维度差异）
            // 我们在 2D 噪声图中，沿着不同的“路径”取值
            float noiseX = Mathf.PerlinNoise(time + seed, 0f);
            float noiseY = Mathf.PerlinNoise(0f, time + seed + 123.45f);

            // 将 0~1 的原始值映射到 -1~1，从而实现以基础点为中心的双向晃动
            float offsetX = (noiseX - 0.5f) * 2f * amp;
            float offsetY = (noiseY - 0.5f) * 2f * amp;

            return basePos + new Vector3(offsetX, offsetY, 0f);
        }

        //初始点位生成
        public Vector3 GetSidePos(
            Vector3 centerPoint,
            float distance,
            bool isRight,
            float angleRange
        )
        {
            // 1. 确定中心角度：右侧为 45度，左侧为 135度
            float centerAngle = isRight ? 15f : 165f;

            // 2. 在范围内随机取一个偏移
            float randomAngle = centerAngle + Random.Range(-angleRange / 2f, angleRange / 2f);

            // 3. 角度转弧度
            float rad = randomAngle * Mathf.Deg2Rad;

            // 4. 计算坐标
            float x = Mathf.Cos(rad) * distance;
            float y = Mathf.Sin(rad) * distance;

            return centerPoint + new Vector3(x, y, 0f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (currentState == BeeState.ThrowedSt && collision.transform.CompareTag("Wall"))
            {
                for (int i = 0, len = collision.contactCount; i < len; i++)
                {
                    Vector2 normal = collision.GetContact(i).normal;
                    if (normal.x <= -0.9f && Mathf.Abs(normal.y) < 0.1f)
                    {
                        ChangeState(BeeState.StaySt);
                    }
                    else if (normal.x >= 0.9f && Mathf.Abs(normal.y) < 0.1f)
                    {
                        ChangeState(BeeState.StaySt);
                    }
                }
            }
            else if (
                currentState == BeeState.ThrowedSt
                && collision.gameObject.layer == LayerMask.GetMask("Hook")
            )
            {
                ChangeState(BeeState.FollowSt);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (currentState == BeeState.StaySt && collision.transform.CompareTag("Wall"))
            {
                ChangeState(BeeState.FollowSt);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Wave"))
            {
                if (currentState == BeeState.StaySt && StayToFollowCdTimer >= StayToFollowCdTime)
                {
                    ChangeState(BeeState.FollowSt);
                    isFollowPlayer = true;
                    MessageBroker.Global.Publish(
                        new BeeManagerEvents.BeeAddEvents(this.gameObject)
                    );
                }
                else if (currentState == BeeState.FollowSt && !isFollowPlayer)
                {
                    isFollowPlayer = true;
                    MessageBroker.Global.Publish(
                        new BeeManagerEvents.BeeAddEvents(this.gameObject)
                    );
                }
            }
        }

        public BeeState currentState;
        public SpriteRenderer m_SpriteRenderer;
        public Rigidbody2D m_RigidBody;
        public BoxCollider2D m_BoxCollider;
        private Transform player;
        private Transform m_Transform;

        [SerializeField]
        private Vector2 PlayerPosition;
        private BeeLdtkControl m_BeeLdtkControl;

        public bool isFollowPlayer = false;
        public Transform FollowCheckPoint;
        public Vector3 CheckPointFollowOffset;

        public float targetDistance() => Vector3.Distance(this.transform.position, FollowPoint);

        private Vector2 targetDir() => (FollowPoint - this.transform.position).normalized;

        public Vector3 FollowPoint;
        public Vector3 testPoint;
        public float FollowPointDistance;
        public float FlashMoveDistance;

        public float FollowSpeedMult;

        public float currentSpeed;

        public int StayToFollowCdTime;
        private int StayToFollowCdTimer;

        private Vector2 myUniqueVar;
        private DisposableBag m_Disposables = new();
    }
}
