using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using R3;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace GameMain.RunTime
{
    public enum TailType
    {
        [LabelText("直线尾巴, 从下到上")]
        Line,

        [LabelText("直角尾巴, 从下到右")]
        Corner,
    }

    public enum SnakeState
    {
        Stay,
        Move,
        Charge,
        Dead,
    }

    public class PlayerSnake : MonoBehaviour
    {
        private void OnEnable()
        {
            MessageBroker
                .Global.Subscribe<GamePlaySnakeGameEvents.SnakeSaveEvent>(_ => OnSave())
                .AddTo(ref m_Disposables);
            MessageBroker
                .Global.Subscribe<SaveSystemEvents.SavePreWriteEvent>(OnBeforeWriteSave)
                .AddTo(ref m_Disposables);
            m_CheckSub =
                MessageBroker.Global.Subscribe<GamePlaySnakeGameEvents.SnakeCheckPointEvent>(
                    OnCheckPoint
                );
        }

        private void OnDisable()
        {
            m_Disposables.Dispose();
            m_CheckSub.Dispose();
        }

        private void Start()
        {
            LoadTailLibrary();
            EnsureTailsContainer();
            if (m_StartPos == Vector3.zero)
                m_StartPos = transform.position;
            m_CheckPos = m_StartPos;

            RestoreFromSave();
        }

        private void Update()
        {
            m_InputAct = Input.GetButton("Act");
            HandleInput();
            StateManager();
            m_LastFrameInputDir = m_InputDir;
        }

        void StateManager()
        {
            //if (m_State == SnakeState.Move && m_InputAct)
            //{
            //    m_State = SnakeState.Charge;
            //    m_Timer = 0;
            //}

            bool dirJustPressed =
                m_InputDir != Vector2Int.zero && m_InputDir != m_LastFrameInputDir;

            if (m_State == SnakeState.Stay)
            {
                m_Timer = 0;
                if (dirJustPressed)
                {
                    m_LastDirection = m_InputDir;
                    m_CurrentDirection = m_InputDir;
                    m_State = SnakeState.Move;
                    Move();
                }
            }
            else if (m_State == SnakeState.Move)
            {
                if (dirJustPressed)
                    Move();
            }
            else if (m_State == SnakeState.Charge)
            {
                if (m_InputAct)
                {
                    m_Timer += Time.deltaTime;
                }
                else if (!m_InputAct)
                {
                    if (m_Timer < m_MinChargeTime)
                    {
                        m_Timer = 0f;
                        m_State = SnakeState.Move;
                    }
                    else if (m_Timer < m_MaxChargeTime)
                    {
                        m_Timer = 0f;
                        m_State = SnakeState.Move;
                        GameObject temp = Instantiate(
                            m_Wave,
                            transform.position,
                            Quaternion.identity
                        );
                        ZoneWaveControl zoneWave = temp.GetComponent<ZoneWaveControl>();
                        zoneWave.rMult = m_Timer - m_MinChargeTime + zoneWave.minMult;
                    }
                    else
                    {
                        m_Timer = 0f;
                        m_State = SnakeState.Move;
                        GameObject temp = Instantiate(
                            m_Wave,
                            transform.position,
                            Quaternion.identity
                        );
                        ZoneWaveControl zoneWave = temp.GetComponent<ZoneWaveControl>();
                        zoneWave.rMult = zoneWave.maxMult;
                    }
                }
            }
            else if (m_State == SnakeState.Dead)
            {
                Die();
            }
        }

        public void SetUp(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            m_StartPos = transform.position;
        }

        private void EnsureTailsContainer()
        {
            if (m_TailContainer == null)
            {
                var go = new GameObject("Snake_Tails_Container");
                m_TailContainer = go.transform;
            }
        }

        private void LoadTailLibrary()
        {
            if (!m_TailLibrary.ContainsKey(TailType.Line))
                m_TailLibrary[TailType.Line] = new List<GameObject>();
            for (int i = 0; i < m_LineTailPathList.Count; i++)
            {
                var assets = Resources.LoadAll<GameObject>(m_LineTailPathList[i]);
                m_TailLibrary[TailType.Line].AddRange(assets);
            }
            //m_TailLibrary[TailType.Line] = Resources.LoadAll<GameObject>(m_LineTailPath).ToList();
            m_TailLibrary[TailType.Corner] = Resources
                .LoadAll<GameObject>(m_CornerTailPath)
                .ToList();

            if (m_TailLibrary[TailType.Line].IsNullOrEmpty())
                CLogger.LogError(
                    $"[Snake] Cannot find LineTail prefab at Resources/{m_LineTailPath}",
                    LogTag.Game
                );

            if (m_TailLibrary[TailType.Corner].IsNullOrEmpty())
                CLogger.LogError(
                    $"[Snake] Cannot find CornerTail prefab at Resources/{m_CornerTailPath}",
                    LogTag.Game
                );
        }

        // ================= 输入 =================
        [SerializeField]
        private Vector2Int? m_PendingDirection;

        [SerializeField]
        private Vector2Int m_InputDir;

        private Vector2Int m_LastFrameInputDir;

        [SerializeField]
        private bool m_InputAct = false;

        private Vector2? HandleInput()
        {
            Vector2 input = VgInput.GetMovementVector();
            m_InputDir = Vector2Int.RoundToInt(input);
            if (input.sqrMagnitude < 0.1f)
                return null;

            Vector2 dir;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                dir = input.x > 0 ? Vector2.right : Vector2.left;
            else
                dir = input.y > 0 ? Vector2.up : Vector2.down;

            m_PendingDirection = Vector2Int.RoundToInt(dir);
            return dir;
        }

        private void UpdateDirection()
        {
            if (m_PendingDirection == null)
                return;

            var next = m_PendingDirection.Value;

            // 防止反向
            if (next + m_CurrentDirection == Vector2Int.zero)
                return;

            m_CurrentDirection = next;
            m_PendingDirection = null;
        }

        // ================= 移动 =================

        private void Move()
        {
            UpdateDirection();

            Vector2Int dir = m_CurrentDirection;

            if (IsBlocked(dir))
            {
                m_State = SnakeState.Dead;
                return;
            }

            Vector3 tailPos = m_Head != null ? m_Head.position : transform.position;
            SpawnTail(tailPos, dir);

            transform.position += new Vector3(dir.x, dir.y, 0);
        }

        private bool IsBlocked(Vector2Int direction)
        {
            Vector3 origin = m_Head != null ? m_Head.position : transform.position;
            Vector2 dir = new(direction.x, direction.y);

            float startOffset = 0f;
            Vector3 rayStart = origin + (Vector3)dir * startOffset;
            float distance = 0.5f;

            Physics2D.SyncTransforms();
            Vector2 boxSize = new Vector2(0.5f, 0.5f); // 假设检测区是 0.5x0.5 的正方形

            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                rayStart,
                boxSize,
                0f,
                dir,
                distance,
                m_ObstacleLayer
            );

            //RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, dir, distance, m_ObstacleLayer);
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider.transform.IsChildOf(transform))
                    continue;

#if UNITY_EDITOR
                CLogger.LogInfo(
                    $"<color=red>[Snake Blocked]</color> Hit: {hit.collider.name} Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)} at {hit.point}",
                    LogTag.Game
                );
#endif

                return true;
            }

            return false;
        }

        // ================= 尾巴 =================

        private Vector2Int m_LastDirection = Vector2Int.up;
        private int lastRandomIndex;

        private TailType GetTailType()
        {
            if (m_LastDirection == m_CurrentDirection)
                return TailType.Line;

            return TailType.Corner;
        }

        private void SpawnTail(Vector3 position, Vector2 towards)
        {
            TailType type = GetTailType();

            if (!m_TailLibrary.TryGetValue(type, out var list) || list.IsNullOrEmpty())
                return;

            Debug.Log(list.Count);
            int randomNum = UnityEngine.Random.Range(0, list.Count);
            if (list.Count > 1)
            {
                do
                {
                    randomNum = UnityEngine.Random.Range(0, list.Count);
                } while (randomNum == lastRandomIndex);
            }

            GameObject prefab = list[randomNum]; // 可改为随机
            lastRandomIndex = randomNum;

            GameObject tailObj = Instantiate(
                prefab,
                position,
                Quaternion.identity,
                m_TailContainer
            );

            SnakeTail tail = tailObj.GetComponent<SnakeTail>();

            tail.Initialize(Vector2.one);
            tail.gameObject.layer = gameObject.layer;
            tail.PrefabIndex = randomNum;
            tail.TailType = type;

            RotateTail(tail, towards);

            m_ActiveTails.Add(tail);

            m_LastDirection = m_CurrentDirection;
        }

        private void RotateTail(SnakeTail tail, Vector2 dir)
        {
            float angle = 0f;

            if (dir == Vector2.up)
                angle = 0;
            else if (dir == Vector2.right)
                angle = -90;
            else if (dir == Vector2.down)
                angle = 180;
            else if (dir == Vector2.left)
                angle = 90;

            tail.transform.rotation = Quaternion.Euler(0, 0, angle);
            if (m_LastDirection != m_CurrentDirection)
            {
                if (dir == Vector2.up && m_LastDirection == Vector2.left)
                    tail.transform.rotation = Quaternion.Euler(0, 180, angle);
                else if (dir == Vector2.right && m_LastDirection == Vector2.up)
                    tail.transform.rotation = Quaternion.Euler(180, 0, angle);
                else if (dir == Vector2.down && m_LastDirection == Vector2.right)
                    tail.transform.rotation = Quaternion.Euler(0, 180, angle);
                else if (dir == Vector2.left && m_LastDirection == Vector2.down)
                    tail.transform.rotation = Quaternion.Euler(180, 0, angle);
            }
        }

        // ================= 生命周期 =================

        private void Die()
        {
            CLogger.LogInfo("Snake Died!", LogTag.Game);
            MessageBroker.Global.Publish(new GamePlaySnakeGameEvents.SnakeDeathEvent());

            //GameObject temp = Instantiate(m_Wave, transform.position, Quaternion.identity);
            //ZoneWaveControl zoneWave = temp.GetComponent<ZoneWaveControl>();
            //zoneWave.rMult = zoneWave.minMult;

            Respawn();

            ResetSnake();
        }

        void Respawn()
        {
            transform.position = m_CheckPos + new Vector3(-0.5f, 0.5f, 0);
            m_State = SnakeState.Stay;
        }

        private void OnSave()
        {
            CLogger.LogInfo("Snake Saved!", LogTag.Game);

            foreach (var tail in m_ActiveTails)
                tail.SetPermanent();

            m_ActiveTails.Clear();
        }

        private void OnCheckPoint(GamePlaySnakeGameEvents.SnakeCheckPointEvent e)
        {
            m_CheckPos = e.Postion;
        }

        private void OnBeforeWriteSave(SaveSystemEvents.SavePreWriteEvent evt)
        {
            if (evt.IsGlobal)
                return;

            var data = new PlayerSnakeSaveData { CheckPos = m_CheckPos };
            if (m_TailContainer != null)
            {
                foreach (Transform child in m_TailContainer)
                {
                    var tail = child.GetComponent<SnakeTail>();
                    if (tail == null)
                        continue;
                    data.Tails.Add(
                        new SnakeTailRecord
                        {
                            Position = child.position,
                            Rotation = child.rotation,
                            Type = tail.TailType,
                            PrefabIndex = tail.PrefabIndex,
                        }
                    );
                }
            }
            VgSaveSystem.Instance.UpdateSaveValue(SaveKeys.PlayerSnake, data);
        }

        private void RestoreFromSave()
        {
            if (!VgSaveSystem.Instance.HasKey(SaveKeys.PlayerSnake))
                return;

            var data = VgSaveSystem.Instance.GetSaveValue<PlayerSnakeSaveData>(
                SaveKeys.PlayerSnake
            );
            if (data == null)
                return;

            m_CheckPos = data.CheckPos;
            transform.position = m_CheckPos + new Vector3(-0.5f, 0.5f, 0);

            if (data.Tails == null)
                return;

            foreach (var record in data.Tails)
            {
                if (
                    !m_TailLibrary.TryGetValue(record.Type, out var list)
                    || list == null
                    || list.Count == 0
                )
                    continue;
                if (record.PrefabIndex < 0 || record.PrefabIndex >= list.Count)
                    continue;

                var prefab = list[record.PrefabIndex];
                GameObject obj = Instantiate(
                    prefab,
                    record.Position,
                    record.Rotation,
                    m_TailContainer
                );
                SnakeTail tail = obj.GetComponent<SnakeTail>();
                if (tail == null)
                    continue;
                tail.Initialize(Vector2.one);
                tail.gameObject.layer = gameObject.layer;
                tail.PrefabIndex = record.PrefabIndex;
                tail.TailType = record.Type;
                tail.SetPermanent();
            }
        }

        private void ResetSnake()
        {
            foreach (var tail in m_ActiveTails)
            {
                if (tail != null)
                {
                    tail.gameObject.SetActive(false);
                    Destroy(tail.gameObject);
                }
            }

            m_ActiveTails.Clear();
            m_Timer = 0;
            m_PendingDirection = null;
            m_CurrentDirection = Vector2Int.up;
            m_LastDirection = Vector2Int.up;
        }

        // ================= 配置 =================

        [SerializeField]
        private float m_MoveInterval = 0.5f;

        [SerializeField]
        private float m_FastMoveInterval = 0.5f;

        [SerializeField]
        private float m_MinChargeTime = 0.5f;

        [SerializeField]
        private float m_MaxChargeTime = 1.5f;

        [SerializeField]
        private List<string> m_LineTailPathList = new();
        private string m_LineTailPath = "SnakeTails/LineTail";

        [SerializeField]
        private string m_CornerTailPath = "SnakeTails/CornerTail";

        [SerializeField]
        private GameObject m_Wave;

        [SerializeField]
        private LayerMask m_ObstacleLayer;

        [SerializeField]
        private Transform m_Head;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField]
        private bool m_ShowDebug = true;

        private void OnDrawGizmos()
        {
            if (!m_ShowDebug)
                return;

            Vector3 origin = m_Head != null ? m_Head.position : transform.position;

            // 绘制当前移动方向 (绿色)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                origin,
                origin + new Vector3(m_CurrentDirection.x, m_CurrentDirection.y, 0)
            );

            // 绘制预输入方向 (黄色)
            if (m_PendingDirection.HasValue)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(
                    origin,
                    origin + new Vector3(m_PendingDirection.Value.x, m_PendingDirection.Value.y, 0)
                );
            }

            // 绘制下一次移动的障碍物检测射线 (红色)
            Gizmos.color = Color.red;
            float distance = 0.8f;
            Vector3 dir = new(m_CurrentDirection.x, m_CurrentDirection.y, 0);
            Gizmos.DrawRay(origin, dir * distance);

            // 绘制射线末端的端点
            Gizmos.DrawWireSphere(origin + dir * distance, 0.05f);
        }
#endif

        // ================= 状态 =================

        private SnakeState m_State = SnakeState.Stay;

        private Vector2Int m_CurrentDirection = Vector2Int.up;
        private float m_Timer;
        private Vector3 m_StartPos;
        private Vector3 m_CheckPos;

        private List<SnakeTail> m_ActiveTails = new();

        [SerializeField]
        private Dictionary<TailType, List<GameObject>> m_TailLibrary = new();

        private DisposableBag m_Disposables = new();
        private IDisposable m_CheckSub;
        private Transform m_TailContainer;
    }
}
