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


			//角色朝向修改
			if (MoveX != 0 && mPCComponent.CurrentState == PlayerStateMachine.NormalState)
				mPCComponent.FacingDir = mPCComponent.MoveX * -1;

			//角色是否能投掷
			if (mPCComponent.CurrentState == PlayerStateMachine.NormalState
				&& IsOnGround)
				mPCComponent.IsCanThrow = true;

			//角色跳跃输入计时器
			if (InputJump && mPCComponent.PreJumpInputTimer > 0)
				mPCComponent.PreJumpInputTimer--;
			else if (!InputJump)
				mPCComponent.PreJumpInputTimer = PreJumpInputTime;
			//角色投掷输入计时器
			if (InputAct && mPCComponent.PreThrowInputTimer > 0)
				mPCComponent.PreThrowInputTimer--;
			else if (!InputAct)
				mPCComponent.PreThrowInputTimer = mPCComponent.PreThrowTime;
			//狼跳计时器
			if (IsOnGround)
				mPCComponent.CoyoteJumpInputRevTimer = CoyoteJumpInputTime;
			else if (mPCComponent.CoyoteJumpInputRevTimer > 0)
				mPCComponent.CoyoteJumpInputRevTimer--;
			//滑落计时器
			if (mPCComponent.CurrentState != PlayerStateMachine.GrabState)
				mPCComponent.GrabStayRevTimer = mPCComponent.GrabStayTime;
			//投掷cd计时器
			if ((mPCComponent.CurrentState != PlayerStateMachine.ThrowState
				|| mPCComponent.CurrentState != PlayerStateMachine.ThrowState)
				&& mPCComponent.ThrowCdInputTimer > 0)
				mPCComponent.ThrowCdInputTimer--;

		}


	}
}
