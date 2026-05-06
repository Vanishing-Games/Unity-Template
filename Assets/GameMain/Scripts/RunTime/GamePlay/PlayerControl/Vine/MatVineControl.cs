using UnityEngine;

namespace GameMain.RunTime
{
    public class MatVineControl : AutoLdtkEntity
    {
        [LDtkField("isVerticalFirst")]
        [SerializeField]
        private bool isVerticalFirst = true;

        [Header("探测设置")]
        public float checkDistance = 0.6f; // 射线探测距离（略大于物体半径）
        public LayerMask wallLayer;
        private Vector3 originalScale;

        [SerializeField]
        private float KeepUnfoldTime;
        private float KeepUnfoldTimer;

        [SerializeField]
        private float shakeTime;

        private BoxCollider2D m_BoxCollider;

        [SerializeField]
        private BoxCollider2D long_BoxCollider;

        [SerializeField]
        private Animator m_Animator;

        [SerializeField]
        private Transform renderTransform;

        private Vector3 initialPosition;

        private bool isUnfold;

        [Header("抖动设置")]
        [Tooltip("抖动的剧烈程度")]
        public float shakeAmount = 0.1f;

        [Tooltip("抖动的频率（速度）")]
        public float shakeSpeed = 20f;
        private float shakeTimer;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            AlignToWall();
            m_BoxCollider = GetComponent<BoxCollider2D>();
            initialPosition = renderTransform.localPosition;
        }

        // Update is called once per frame
        void Update()
        {
            if (isUnfold)
            {
                KeepUnfoldTimer += Time.deltaTime;
                if (KeepUnfoldTimer > KeepUnfoldTime + shakeTime)
                {
                    ChangeToFold();
                }
                else if (KeepUnfoldTimer > KeepUnfoldTime)
                {
                    PlatformShake();
                }
            }
        }

        void PlatformShake()
        {
            shakeTimer += Time.deltaTime * shakeSpeed;

            float x = (Mathf.PerlinNoise(shakeTimer, 0f) - 0.5f) * 2;
            float y = (Mathf.PerlinNoise(0f, shakeTimer) - 0.5f) * 2;

            Vector3 randomOffset = new Vector3(x, y, 0) * shakeAmount;

            renderTransform.localPosition = initialPosition + randomOffset;
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
                    finalScale = originalScale; // 默认即贴右墙
                    targetAngle = 0f;
                    transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer(
                        "HorizontalGrab"
                    );
                }
                else if (hitRight)
                {
                    finalScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
                    targetAngle = 0f;
                    transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer(
                        "HorizontalGrab"
                    );
                }
                else if (hitUp)
                    targetAngle = -90f;
                else if (hitDown)
                {
                    targetAngle = 90f;
                }
            }
            else
            {
                // 优先处理上下
                if (hitUp)
                    targetAngle = -90f;
                else if (hitDown)
                {
                    targetAngle = 90f;
                }
                else if (hitLeft)
                {
                    finalScale = originalScale;
                    targetAngle = 0f;
                    transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer(
                        "HorizontalGrab"
                    );
                }
                else if (hitRight)
                {
                    finalScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
                    targetAngle = 0f;
                    transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer(
                        "HorizontalGrab"
                    );
                }
            }
            transform.localScale = finalScale;
            transform.rotation = Quaternion.Euler(0, 0, targetAngle);
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

        void ChangeToFold()
        {
            isUnfold = false;
            long_BoxCollider.enabled = false;
            m_BoxCollider.enabled = true;
            KeepUnfoldTimer = 0;
            m_Animator.SetBool("IsUnfold", isUnfold);
        }

        void ChangeToUnfold()
        {
            isUnfold = true;
            long_BoxCollider.enabled = true;
            m_BoxCollider.enabled = false;
            m_Animator.SetBool("IsUnfold", isUnfold);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Wave") && !isUnfold)
            {
                ChangeToUnfold();
            }
        }
    }
}
