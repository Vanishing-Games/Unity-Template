using System.Collections.Generic;
using Core;
using R3;
using UnityEngine;

namespace GameMain.RunTime
{
    public class MatCheckpointControl : AutoLdtkEntity
    {
        [SerializeField]
        private GameObject WavePre;

        [LDtkField]
        public string PointName;

        private bool isBloom = false;
        private bool isChecking = false;

        private Animator m_Animator;

        private DisposableBag m_Disposables = new();

        private void Awake()
        {
            if (TryGetComponent<BoxCollider2D>(out var col))
            {
                col.isTrigger = true;
                col.size = BaseGridSize;
            }
            m_Animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            MessageBroker
                .Global.Subscribe<GamePlayMatEvents.MatChangeCheckPointEvent>(_ => OnCheck())
                .AddTo(ref m_Disposables);
        }

        private void OnDisable()
        {
            m_Disposables.Dispose();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start() { }

        // Update is called once per frame
        void Update()
        {
            m_Animator.SetBool("IsBloom", isBloom);
            m_Animator.SetBool("IsChecking", isChecking);
        }

        public override void OnPostImport()
        {
            // 如果 LDtk 没有配置名字，则自动生成一个唯一的关卡内名字
            if (string.IsNullOrEmpty(PointName))
            {
                string worldId = World != null ? World.Identifier : "World";
                string levelId = Level != null ? Level.Identifier : "Level";
                PointName = $"{worldId}_{levelId}_{LdtkIid}";
            }
        }

        void OnCheck()
        {
            if (isChecking)
                isChecking = false;
        }

        void OnFirstCheck()
        {
            //之后修改Wave的参数
            if (WavePre != null)
                Instantiate(WavePre, this.transform);
            OnCheckTrigger();
            isBloom = true;
            isChecking = true;
        }

        void OnReCheck()
        {
            OnCheckTrigger();
            isChecking = true;
            //if (isChecking == false)
            //         {
            //             OnCheckTrigger();
            //             isChecking = true;
            //         }
            //         else
            //         {
            //             OnCheckTrigger();
            //             isChecking = true;
            //         }
        }

        void OnCheckTrigger()
        {
            MessageBroker.Global.Publish(
                new GamePlayMatEvents.MatChangeCheckPointEvent(this.transform)
            );
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (isBloom == false)
                {
                    OnFirstCheck();
                }
                else if (isBloom)
                {
                    OnReCheck();
                }
            }
        }
    }
}
