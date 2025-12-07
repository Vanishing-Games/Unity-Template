using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
	public class PlayerControlDash : PlayerControlCapabilityBase
	{
		protected override void SetUpTickSettings()
		{
			base.SetUpTickSettings();
			TickOrderInGroup = (uint)PlayerControlTickOrder.DashControl;
		}

		protected override bool OnShouldActivate()
		{
			return mPCComponent.CurrentState == PlayerStateMachine.DashState;
		}

		protected override void OnActivate()
		{
			DashDir = (mPCComponent.ThrownHook.transform.position -
				mPCComponent.mTranform.position).normalized;
			mPCComponent.DashTimer = mPCComponent.DashTime;
			mPCComponent.DashWaitTimer = mPCComponent.DashWaitTime;
		}

		protected override bool OnShouldDeactivate()
		{
			return mPCComponent.DashTimer == 0;
		}

		protected override void OnDeactivate()
		{
			SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
		}

		protected override void OnTick(float deltaTime)
		{
			if (mPCComponent.DashWaitTimer > 0)
			{
				mPCComponent.DashWaitTimer--;
				mPCComponent.CtrlVelocity = Vector2.zero;
			}

			if (mPCComponent.DashTimer > 0 && mPCComponent.DashWaitTimer == 0)
			{
				if (mPCComponent.DashTimer > mPCComponent.DashEndSlowTime)
				{
					mPCComponent.CtrlVelocity = DashDir * mPCComponent.DashSpeed;
				}
				else
				{
					mPCComponent.CtrlVelocity = ApproachInTime(
						mPCComponent.CtrlVelocity,
						mPCComponent.DashSpeed * mPCComponent.EndSlowMult,
						mPCComponent.DashTimer);
				}
				mPCComponent.DashTimer--;
			}
		}

		private Vector2 DashDir;

	}
}
