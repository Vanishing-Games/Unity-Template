using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;
using Core;
using System;

namespace PlayerControlByOris
{
	public class PlayerStateStartSet : PlayerControlCapabilityBase
	{
		protected override void SetUpTickSettings()
		{
			base.SetUpTickSettings();
			TickOrderInGroup = (uint)PlayerControlTickOrder.StateStartSet;
			
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
			//MoveX管理
			if (mPCComponent.ForceMoveXRevTimer > 0)
			{
				mPCComponent.MoveX = mPCComponent.ForceMoveX;
				mPCComponent.ForceMoveXRevTimer--;
			}
			else
			{
				mPCComponent.MoveX = (int)Math.Sign(mPCComponent.InputX);
			}

			mPCComponent.FacingDir = 1;
			//角色朝向修改
			if (MoveX != 0)
				mPCComponent.FacingDir = mPCComponent.MoveX * -1;

			//角色跳跃输入计时器
			if (InputJump && mPCComponent.PreJumpInputTimer > 0)
				mPCComponent.PreJumpInputTimer--;
			else if (!InputJump)
				mPCComponent.PreJumpInputTimer = PreJumpInputTime;
			//狼跳计时器
			if (IsOnGround)
				mPCComponent.CoyoteJumpInputRevTimer = CoyoteJumpInputTime;
			else if (mPCComponent.CoyoteJumpInputRevTimer > 0)
				mPCComponent.CoyoteJumpInputRevTimer--;
		}


	}
}
