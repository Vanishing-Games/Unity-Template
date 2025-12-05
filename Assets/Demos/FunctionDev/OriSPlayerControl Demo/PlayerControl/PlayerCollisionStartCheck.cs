using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;
using Core;
using System;

namespace PlayerControlByOris
{
	public class PlayerCollisionStartCheck : PlayerControlCapabilityBase
	{
		protected override void SetUpTickSettings()
		{
			base.SetUpTickSettings();
			TickOrderInGroup = (uint)PlayerControlTickOrder.CollisionStartCheck;
		}

		protected override void OnSetup()
		{
			//SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
		}

		protected override bool OnShouldActivate()
		{
			return true;
		}

		protected override bool OnShouldDeactivate()
		{
			return false;
		}

		protected override void OnTick(float deltaTime)
		{
			if (!IsOnGround
				&& mPCComponent.CtrlVelocity.y < LowGravThresholdSpeedY)
			{
				//Debug.Log(mPCComponent.mTranform.position);
				CornerGrabCheck(mPCComponent.mTranform.position, Vector2.right);
				CornerGrabCheck(mPCComponent.mTranform.position, Vector2.left);
			}
		}

		private void CornerGrabCheck(Vector2 PlayerPosition, Vector2 Dir)
		{
			Vector2 StartPoint =PlayerPosition +
				PlayerColliderOffsetX * Dir + new Vector2(0, PlayerColliderOffsetUpY);
			RaycastHit2D HeadHit = Physics2D.Raycast(
				StartPoint - new Vector2(0, VerticalDistance),
				Dir,
				HorizontalDistance,
				LayerMask.GetMask("Wall")
				);
			Debug.DrawRay(StartPoint - new Vector2(0, VerticalDistance), Dir * HorizontalDistance, Color.red);

			if (HeadHit.collider == null)
				return;

			Vector2 DownStartPoint = HeadHit.point + (Vector2.up * VerticalDistance * 2);

			RaycastHit2D DownHit = Physics2D.Raycast(
				DownStartPoint,
				Vector2.down,
				VerticalDistance * 2f,
				LayerMask.GetMask("Wall")
				);
			Debug.Log(DownHit.collider == null);

			Debug.DrawRay(DownStartPoint, Vector2.down * VerticalDistance, Color.yellow);
			//Debug.Log("2");
			if (DownHit.collider)
			{
				
				if (Vector2.SqrMagnitude(DownHit.point - DownStartPoint) > 0)
				{					
					Vector2 targetPoint =DownHit.point -
						PlayerColliderOffsetX * Dir - new Vector2(0, PlayerColliderOffsetUpY);
					GrabSet(targetPoint, true, false, Dir);
				}
			}
		}

		private void GrabSet(Vector2 targetPoint, bool IsCorner, bool IsSafe, Vector2 dir)
		{
			SetStateMachine(PlayerStateMachine.GrabState, EccTag.GrabState);
			mPCComponent.IsCornerGrab = IsCorner;
			mPCComponent.IsSafeGrab = IsSafe;
			mPCComponent.CtrlVelocity = Vector2.zero;

			//mPCComponent.FacingDir = (int)dir.x * -1;
			mPCComponent.mTranform.position = targetPoint;
		}


		private float PlayerColliderOffsetX => mPCComponent.mBoxCollider.size.x * 0.5f;
		private float PlayerColliderOffsetUpY => mPCComponent.mBoxCollider.size.y * 0.5f + mPCComponent.mBoxCollider.offset.y;



		private float HorizontalDistance => mPCComponent.CornerGrabOffsetX;
		private float VerticalDistance => mPCComponent.CornerGrabOffsetY;
	}
}
