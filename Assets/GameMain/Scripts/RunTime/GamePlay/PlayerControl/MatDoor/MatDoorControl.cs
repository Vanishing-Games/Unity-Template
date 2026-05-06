using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace GameMain.RunTime
{
    public class MatDoorControl : AutoLdtkEntity
    {
        [LDtkField("LockCount")]
        [SerializeField]
        private int LockCount = 1;

        private int m_UnlockCount = 0;

        private bool isUnlock = false;
        private bool isCanClose = true;

        [SerializeField]
        private float ColdDownTime;
        private float ColdDownTimer;

        [SerializeField]
        private float CloseDownTime;
        private float CloseDownTimer;

        [SerializeField]
        private SpriteRenderer doorRenderer;
        private Animator m_Animator;

        private BoxCollider2D m_BoxCollider;

        private HashSet<int> processedWaveIDs = new HashSet<int>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_BoxCollider = GetComponent<BoxCollider2D>();
            m_Animator = GetComponentInChildren<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            m_Animator.SetInteger("TutalCount", LockCount);
            m_Animator.SetInteger("CurrentCount", m_UnlockCount);

            if (m_UnlockCount >= LockCount)
            {
                isUnlock = true;
                //doorRenderer.enabled = false;
                m_BoxCollider.isTrigger = true;
            }

            if (m_UnlockCount > 0 && isUnlock == false)
            {
                ColdDownTimer += Time.deltaTime;
                if (ColdDownTimer > ColdDownTime)
                {
                    ColdDownClear();
                }
            }

            if (!isUnlock)
            {
                //doorRenderer.enabled = true;
                m_BoxCollider.isTrigger = false;
            }
        }

        void CloseDown()
        {
            isUnlock = false;
            m_UnlockCount = 0;
            CloseDownTimer = 0;
            ColdDownTimer = 0;
        }

        void ColdDownClear()
        {
            m_UnlockCount = 0;
            ColdDownTimer = 0;
            processedWaveIDs.Clear();
        }

        void UnlockUp()
        {
            m_UnlockCount++;
            ColdDownTimer = 0;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Wave") && !isUnlock)
            {
                WaveLife wave = collision.GetComponent<WaveLife>();
                if (!processedWaveIDs.Contains(wave.sourceID))
                {
                    processedWaveIDs.Add(wave.sourceID);
                    UnlockUp();
                }
            }

            if (collision.transform.CompareTag("Player"))
            {
                MessageBroker.Global.Publish(new GamePlayMatEvents.MatPlayerPassDoorEvent());
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Player"))
            {
                isCanClose = false;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Player"))
            {
                isCanClose = true;
            }
        }
    }
}
