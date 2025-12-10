using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using R3.Triggers;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
	public partial class PlayerControlComponent : EccComponent
	{
		protected override void OnSetup()
		{
			mRigidbody = mGameObject.GetComponent<Rigidbody2D>();
			mAnim = mGameObject.GetComponent<Animator>();
			mBoxCollider = mGameObject.GetComponent<BoxCollider2D>();
			mTranform = mGameObject.GetComponent<Transform>();

			mCollisionEnterSubscription = mGameObject
				.OnCollisionEnter2DAsObservable()
				.Subscribe(OnCollisionEnter2D);
			mCollisionExitSubscription = mGameObject
				.OnCollisionExit2DAsObservable()
				.Subscribe(OnCollisionExit2D);
			mCollisionStaySubscription = mGameObject
				.OnCollisionStay2DAsObservable()
				.Subscribe(OnCollisionStay2D);
		}

		protected override void OnRemoved()
		{
			mCollisionEnterSubscription.Dispose();
			mCollisionExitSubscription.Dispose();
			mCollisionStaySubscription.Dispose();
		}

		protected override void OnUpdateGo(float deltaTime)
		{
			AnimControl();
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			if (collision.transform.CompareTag("Wall"))
			{
				Vector2 normal = collision.GetContact(0).normal;
				if (IsNormalUp(normal))
				{
					IsOnGround = true;
					if (CtrlVelocity.y < 0)
						CtrlVelocity = new Vector2(CtrlVelocity.x, 0);
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

					if (IsNormalUp(normal))
					{
						IsOnGround = true;
						if (CtrlVelocity.y < 0)
							CtrlVelocity = new Vector2(CtrlVelocity.x, 0);
					}
					else if (IsNormalDown(normal))
					{
						IsJumping = false;
						if (CtrlVelocity.y > 0)
							CtrlVelocity = new Vector2(CtrlVelocity.x, 0);
					}
					else if (IsNormalLeft(normal))
					{
						if (CtrlVelocity.y > 0)
							IsOnGround = false;
						if (CtrlVelocity.x > 0)
							CtrlVelocity = new Vector2(0, CtrlVelocity.y);
					}
					else if (IsNormalRight(normal))
					{
						if (CtrlVelocity.y > 0)
							IsOnGround = false;
						if (CtrlVelocity.x < 0)
							CtrlVelocity = new Vector2(0, CtrlVelocity.y);
					}
				}
			}
		}

		private void OnCollisionExit2D(Collision2D collision)
		{
			if (collision.transform.CompareTag("Wall"))
				IsOnGround = false;
		}

		private bool IsNormalUp(Vector2 normal) => normal.y >= 0.9f && Mathf.Abs(normal.x) < 0.1f;
		private bool IsNormalDown(Vector2 normal) => normal.y <= -0.9f && Mathf.Abs(normal.x) < 0.1f;
		private bool IsNormalLeft(Vector2 normal) => normal.x <= -0.9f && Mathf.Abs(normal.y) < 0.1f;
		private bool IsNormalRight(Vector2 normal) => normal.x >= 0.9f && Mathf.Abs(normal.y) < 0.1f;


		[HideInInspector]
		public Transform mTranform;
		[HideInInspector]
		public Rigidbody2D mRigidbody;
		[HideInInspector]
		public BoxCollider2D mBoxCollider;
		[HideInInspector]
		public Animator mAnim;
		[HideInInspector]
		public GameObject mGO;
		private IDisposable mCollisionEnterSubscription;
		private IDisposable mCollisionExitSubscription;
		private IDisposable mCollisionStaySubscription;
	}
}
