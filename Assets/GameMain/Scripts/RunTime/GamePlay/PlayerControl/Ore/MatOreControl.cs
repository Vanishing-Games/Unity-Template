using System.Collections;
using System.Threading;
using UnityEngine;

namespace GameMain.RunTime
{
    public class MatOreControl : AutoLdtkEntity
    {
        [LDtkField("isVerticalFirst")]
        [SerializeField]
        private bool isVerticalFirst = true;

        [Header("探测设置")]
        public float checkDistance = 0.6f; // 射线探测距离（略大于物体半径）
        public LayerMask wallLayer;

        [SerializeField]
        private GameObject WavePre;

        [SerializeField]
        private float ColdDownTime;
        private float ColdDownTimer;

        private Vector3 originalScale;

        private Animator m_Animator;

        private bool isOn = false;

        [Header("透明度速度设置")]
        public float fadeToZeroSpeed = 1.0f; // 变透明的速度（开）
        public float fadeToOneSpeed = 2.0f; // 恢复不透明的速度（关）

        private SpriteRenderer childRenderer;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            AlignToWall();
            childRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            if (isOn)
            {
                ColdDownTimer += Time.deltaTime;
                if (ColdDownTimer > ColdDownTime)
                {
                    ChangeToOff();
                }
            }

            if (childRenderer != null)
            {
                float targetAlpha = isOn ? 1f : 0f;

                // 如果当前透明度还没达到目标值，且没有协程在跑，或者目标变了，就开启/重启协程
                if (!Mathf.Approximately(childRenderer.color.a, targetAlpha))
                {
                    if (fadeCoroutine != null)
                        StopCoroutine(fadeCoroutine);
                    fadeCoroutine = StartCoroutine(DoFade(targetAlpha));
                }
            }
        }

        public void AlignToWall()
        {
            // 探测四个方向是否有墙
            bool hitLeft = CheckWall(Vector2.left);
            bool hitRight = CheckWall(Vector2.right);
            bool hitUp = CheckWall(Vector2.up);
            bool hitDown = CheckWall(Vector2.down);

            Vector3 finalScale = originalScale;
            float targetAngle = 0f;

            if (isVerticalFirst)
            {
                // 优先处理左右
                if (hitLeft)
                {
                    finalScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
                    targetAngle = 0f;
                    transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Hook");
                }
                else if (hitRight)
                {
                    finalScale = originalScale; // 默认即贴右墙
                    targetAngle = 0f;
                    transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Hook");
                }
                else if (hitUp)
                    targetAngle = 90f;
                else if (hitDown)
                {
                    targetAngle = -90f;
                    finalScale = new Vector3(originalScale.x, -originalScale.y, originalScale.z);
                }
            }
            else
            {
                // 优先处理上下
                if (hitUp)
                    targetAngle = 90f;
                else if (hitDown)
                {
                    targetAngle = -90f;
                    finalScale = new Vector3(originalScale.x, -originalScale.y, originalScale.z);
                }
                else if (hitLeft)
                {
                    finalScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
                    targetAngle = 0f;
                    transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Hook");
                }
                else if (hitRight)
                {
                    finalScale = originalScale;
                    targetAngle = 0f;
                    transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Hook");
                }
            }
            transform.localScale = finalScale;
            transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }

        IEnumerator DoFade(float targetAlpha)
        {
            // 根据目标决定使用哪种速度
            float currentSpeed = isOn ? fadeToOneSpeed : fadeToZeroSpeed;

            while (!Mathf.Approximately(childRenderer.color.a, targetAlpha))
            {
                Color c = childRenderer.color;
                // 使用 MoveTowards 确保平滑变化，且不会超过目标值
                c.a = Mathf.MoveTowards(c.a, targetAlpha, currentSpeed * Time.deltaTime);
                childRenderer.color = c;

                // 关键：在循环中实时检测开关是否突变，如果突变则跳出，让 Update 重新分配协程
                float nextTarget = isOn ? 1f : 0f;
                if (!Mathf.Approximately(targetAlpha, nextTarget))
                    yield break;

                yield return null;
            }
        }

        bool CheckWall(Vector2 direction)
        {
            // 发射射线探测墙壁
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                direction,
                checkDistance,
                wallLayer
            );

            // 调试用：在编辑器窗口画出射线
            Debug.DrawRay(
                transform.position,
                direction * checkDistance,
                hit.collider ? Color.green : Color.red,
                2f
            );

            return hit.collider != null;
        }

        void ChangeToOff()
        {
            isOn = false;
        }

        void ChangeToOn()
        {
            isOn = true;
            ColdDownTimer = 0;
            if (WavePre != null)
            {
                Instantiate(WavePre, transform.position, Quaternion.identity);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Wave") && !isOn)
            {
                ChangeToOn();
            }
        }
    }
}
