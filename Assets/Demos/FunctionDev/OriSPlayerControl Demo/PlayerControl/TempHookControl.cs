using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerControlByOris
{
    public class TempHookControl : MonoBehaviour
    {
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // Update is called once per frame
        void Update() { }

        private void FixedUpdate()
        {
            if (isOnWall)
                RopeCreate();
            rb.velocity = rVelocity;
        }

        public void DestroyThis()
        {
            Destroy(this.gameObject);
        }

        void RopeCreate()
        {
            if (tempRope == null)
            {
                tempRope = Instantiate(ropePre);
                tempRope.transform.SetParent(transform);
                if (WallDir == new Vector2(0, -1))
                    tempRope.transform.localPosition = downOffset;
                else if (WallDir == new Vector2(1, 0))
                    tempRope.transform.localPosition = rightOffset;
                else if (WallDir == new Vector2(-1, 0))
                    tempRope.transform.localPosition = leftOffset;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.transform.CompareTag("Wall"))
            {
                for (int i = 0, len = collision.contactCount; i < len; i++)
                {
                    Vector2 normal = collision.GetContact(i).normal;
                    if (normal.x <= -0.9f && Mathf.Abs(normal.y) < 0.1f)
                    {
                        isOnWall = true;
                        rVelocity = Vector2.zero;
                        WallDir = new Vector2(-1, 0);
                    }
                    else if (normal.x >= 0.9f && Mathf.Abs(normal.y) < 0.1f)
                    {
                        isOnWall = true;
                        rVelocity = Vector2.zero;
                        WallDir = new Vector2(1, 0);
                    }
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.transform.CompareTag("Wall"))
            {
                for (int i = 0, len = collision.contactCount; i < len; i++)
                {
                    Vector2 normal = collision.GetContact(i).normal;
                    if (normal.x <= -0.9f && Mathf.Abs(normal.y) < 0.1f)
                    {
                        isOnWall = true;
                        rVelocity = Vector2.zero;
                        WallDir = new Vector2(-1, 0);
                    }
                    else if (normal.x >= 0.9f && Mathf.Abs(normal.y) < 0.1f)
                    {
                        isOnWall = true;
                        rVelocity = Vector2.zero;
                        WallDir = new Vector2(1, 0);
                    }
                }
            }
        }

        public bool isDestroy;
        private Rigidbody2D rb;
        private GameObject tempRope;
        public GameObject ropePre;
        public Vector2 WallDir;
        public bool isOnWall;
        public Vector2 rVelocity;
        public Vector2 ThrowVelocity;
        public Vector2 downOffset;
        public Vector2 rightOffset;
        public Vector2 leftOffset;
    }
}
