using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
	public class PlayerControlJump : PlayerControlCapabilityBase
	{
		protected override void SetUpTickSettings()
		{
			base.SetUpTickSettings();
			TickOrderInGroup = (uint)PlayerControlTickOrder.JumpControl;
			Tags = new List<EccTag> { EccTag.NormalState };
		}
		protected override void OnActivate()
		{
			mPCComponent.JumpingTimer = 0;
			mPCComponent.IsJumping = true;
			mPCComponent.IsOnGround = false;

			Vector2 velocity = mPCComponent.CtrlVelocity;
			velocity.x += MoveX * JumpBoostSpeedX;
			mPCComponent.CtrlVelocity = velocity;

			mPCComponent.CoyoteJumpInputRevTimer = 0;
		}

		protected override void OnDeactivate()
		{
			mPCComponent.JumpingTimer = 0;
			mPCComponent.IsJumping = false;
		}

		protected override bool OnShouldActivate()
		{
			return IsReadyJump();
		}

		protected override bool OnShouldDeactivate()
		{
			return IsEndJump();
		}

		protected override void OnTick(float deltaTime)
		{
			Vector2 velocity = mPCComponent.CtrlVelocity;
			velocity.y = JumpSpeedY;
			mPCComponent.JumpingTimer++;
			mPCComponent.CtrlVelocity = velocity;
		}

		protected bool IsReadyJump()
		{
			if (IsOnGround && mPCComponent.PreJumpInputTimer > 0
				&& mPCComponent.PreJumpInputTimer < PreJumpInputTime)
				return true;
			else if (!IsOnGround && mPCComponent.PreJumpInputTimer > 0
				&& mPCComponent.PreJumpInputTimer < PreJumpInputTime
				&& mPCComponent.CoyoteJumpInputRevTimer > 0)
				return true;
			else if (IsJumping)
				return true;
			else
				return false;
		}

		protected bool IsEndJump()
		{
			if (mPCComponent.JumpingTimer > MinJumpTime && !InputJump)
				return true;
			else if (mPCComponent.JumpingTimer >= MaxJumpTime)
				return true;
			else if (CollisionEndJump())
				return true;
			else
				return false;
		}

		protected bool CollisionEndJump() => !IsJumping;
	}


}
