using UnityEngine;

namespace GameMain.RunTime
{
    public class MatCollectionControl : AutoLdtkEntity
    {
        private bool isBloom;

        private Animator m_Animator;

        void Start()
        {
            m_Animator = GetComponentInChildren<Animator>();
        }

        void Update() { }

        void ChangeToBloom()
        {
            isBloom = true;
            m_Animator.SetBool("IsBloom", isBloom);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag("Wave") && !isBloom)
            {
                ChangeToBloom();
            }
        }
    }
}
