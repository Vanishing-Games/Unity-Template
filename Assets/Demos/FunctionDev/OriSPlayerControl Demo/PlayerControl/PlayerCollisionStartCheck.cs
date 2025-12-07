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
			base.OnSetup();
			
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
			if (IsCanGrab())
			{			
				CornerGrabCheck(mPCComponent.mTranform.position, Vector2.right);
				CornerGrabCheck(mPCComponent.mTranform.position, Vector2.left);
			}

			if ((!mPCComponent.IsCornerGrab &&
				mPCComponent.CurrentState == PlayerStateMachine.GrabState) ||
				mPCComponent.CurrentState == PlayerStateMachine.NormalState
				)
				NormalGrabCheck(mPCComponent.mTranform.position);
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

			Vector2 DownStartPoint = HeadHit.point + (Vector2.up * VerticalDistance * 2)
				+ Dir * 0.1f;

			RaycastHit2D DownHit = Physics2D.Raycast(
				DownStartPoint,
				Vector2.down,
				VerticalDistance * 2f,
				LayerMask.GetMask("Wall")
				);
			Debug.DrawRay(DownStartPoint, Vector2.down * VerticalDistance, Color.yellow);
			if (DownHit.collider)
			{
				Debug.Log(DownHit.point);
				Debug.Log(DownStartPoint);
				if (Vector2.Distance(DownHit.point, DownStartPoint) > 0.01)
				{					
					Vector2 targetPoint =DownHit.point -
						PlayerColliderOffsetX * Dir -
						new Vector2(0, PlayerColliderOffsetUpY);
					Debug.Log(targetPoint);
					Debug.Log(DownHit.point);
					GrabSet(targetPoint, true, false, Dir);
				}
			}
		}

		private void NormalGrabCheck(Vector2 PlayerPosition)
		{
			Vector2 BoxRange = new Vector2(mPCComponent.GrabRangeX, mPCComponent.GrabRangeY);
			Collider2D hitCollider = Physics2D.OverlapBox(
				PlayerPosition + mPCComponent.GrabRangeOffset,
				BoxRange,
				0f,
				LayerMask.GetMask("VerticalGrab")
				);

			if (hitCollider != null && IsCanGrab())
			{
				Bounds GrabBounds = hitCollider.bounds;
				float centerX = (float)GrabBounds.center.x;
				Vector2 targetPoint = new Vector2(centerX, PlayerPosition.y);
				GrabSet(targetPoint, false, false, new Vector2(-1 * mPCComponent.FacingDir, 0));
			}
			else if (hitCollider == null && mPCComponent.CurrentState == PlayerStateMachine.GrabState)
			{
				SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
			}
		}

		private void GrabSet(Vector2 targetPoint, bool IsCorner, bool IsSafe, Vector2 dir)
		{
			SetStateMachine(PlayerStateMachine.GrabState, EccTag.GrabState);
			mPCComponent.CurrentState = PlayerStateMachine.GrabState;
			mPCComponent.IsCornerGrab = IsCorner;
			mPCComponent.IsSafeGrab = IsSafe;
			mPCComponent.CtrlVelocity = Vector2.zero;

			mPCComponent.FacingDir = (int)dir.x * -1;
			mPCComponent.mTranform.position = targetPoint;
		}

		private bool IsCanGrab() => !IsOnGround
			&& mPCComponent.CurrentState == PlayerStateMachine.NormalState
			&& mPCComponent.CtrlVelocity.y < mPCComponent.GrabThresholdSpeedY;
		private float PlayerColliderOffsetX => mPCComponent.mBoxCollider.size.x * 0.5f;
		private float PlayerColliderOffsetUpY => mPCComponent.mBoxCollider.size.y * 0.5f + mPCComponent.mBoxCollider.offset.y;



		private float HorizontalDistance => mPCComponent.CornerGrabOffsetX;
		private float VerticalDistance => mPCComponent.CornerGrabOffsetY;

		
	}
}
