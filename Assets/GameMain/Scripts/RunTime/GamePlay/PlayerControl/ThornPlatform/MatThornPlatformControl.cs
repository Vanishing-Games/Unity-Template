using System;
using UnityEngine;

namespace GameMain.RunTime
{
    public class MatThornPlatformControl : AutoLdtkEntity
    {
        private BoxCollider2D m_BoxCollider2d;
        private Animator m_Animator;
        private Transform renderTransform;

        private bool isPlatform = false;
        private Vector2 platformSize = new Vector2(3, 1);
        private Vector2 thornPlusSize = new Vector2(0.5f, 0.5f);

        [SerializeField]
        private float KeepTime;

        [SerializeField]
        private float ShakeTime;

        private float m_KeepTimer;

        [Header("抖动设置")]
        [Tooltip("抖动的剧烈程度")]
        public float shakeAmount = 0.1f;

        [Tooltip("抖动的频率（速度）")]
        public float shakeSpeed = 20f;
        private float shakeTimer;

        private Vector3 initialPosition;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_BoxCollider2d = GetComponent<BoxCollider2D>();
            m_Animator = GetComponentInChildren<Animator>();
            renderTransform = GetComponentInChildren<Transform>();
            initialPosition = renderTransform.localPosition;
            ChangeToThron();
        }

        // Update is called once per frame
        void Update()
        {
            if (isPlatform)
            {
                m_KeepTimer += Time.deltaTime;
                if (m_KeepTimer >= KeepTime && m_KeepTimer <= KeepTime + ShakeTime)
                {
                    PlatformShake();
                }
                else if (m_KeepTimer > KeepTime + ShakeTime)
                {
                    ChangeToThron();
                }
            }
        }

        void PlatformShake()
        {
            shakeTimer += Time.deltaTime * shakeSpeed;

            float x = (Mathf.PerlinNoise(shakeTimer, 0f) - 0.5f) * 2;
            float y = (Mathf.PerlinNoise(0f, shakeTimer) - 0.5f) * 2;

            Vector3 randomOffset = new Vector3(x, y, 0) * shakeAmount;

            randomOffset.y = 0;
            renderTransform.localPosition = initialPosition + randomOffset;
        }

        void ChangeToPlatform()
        {
            isPlatform = true;
            m_BoxCollider2d.size = platformSize;
            m_BoxCollider2d.isTrigger = false;
            m_Animator.SetBool("IsPlatform", isPlatform);
            gameObject.tag = "Wall";
        }

        void ChangeToThron()
        {
            renderTransform.localPosition = initialPosition;
            isPlatform = false;
            m_KeepTimer = 0;
            m_BoxCollider2d.size = thornPlusSize + platformSize;
            m_BoxCollider2d.isTrigger = true;
            m_Animator.SetBool("IsPlatform", isPlatform);
            gameObject.tag = "Danger";
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Wave") && !isPlatform)
            {
                ChangeToPlatform();
            }
        }
    }
}
