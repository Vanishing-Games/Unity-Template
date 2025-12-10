using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;


namespace PlayerControlByOris
{
	public class PlayerStMachineThrow : PlayerControlCapabilityBase
	{
		protected override void SetUpTickSettings()
		{
			base.SetUpTickSettings();
			TickOrderInGroup = (uint)PlayerControlTickOrder.ThrowControl;
		}

		protected override bool OnShouldActivate()
		{
			return CanThrowCheck();
		}

		protected override void OnActivate()
		{
			SetStateMachine(PlayerStateMachine.ThrowState, EccTag.ThrowState);
			mPCComponent.ThrowCdInputTimer = mPCComponent.ThrowCdTime;
			mPCComponent.ThrowStartTimer = mPCComponent.ThrowStartTime;
			mPCComponent.ThrowMoveTimer = mPCComponent.ThrowMoveTime;
		}

		protected override bool OnShouldDeactivate()
		{
			return ThrowMoveTimer == 0;
		}

		protected override void OnDeactivate()
		{
			IsHookThing = TempHC.isOnWall;
			if (IsHookThing)
			{
				SetStateMachine(PlayerStateMachine.DashState, EccTag.DashState);
			}
			else
			{
				SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
				TempHC.DestroyThis();
				TempHC = null;
				mPCComponent.ThrownHook = null;
			}
		}

		protected override void OnTick(float deltaTime)
		{
			if (ThrowStartTimer > 0)
			{
				mPCComponent.ThrowStartTimer--;
				ThrowStartGoing();
			}

			if (ThrowStartTimer == 0 && ThrowMoveTimer == mPCComponent.ThrowMoveTime)
			{
				CreateHook();
			}

			if (ThrowStartTimer == 0 && ThrowMoveTimer > 0)
			{
				mPCComponent.ThrowMoveTimer--;
				ThrowMoveGoing();
			}
		}

		private void CreateHook()
		{
			if (mPCComponent.ThrownHook != null)
			{
				TempHC.DestroyThis();
			}
			mPCComponent.ThrownHook = Object.Instantiate(mPCComponent.PreHook);
			mPCComponent.ThrownHook.transform.position = mPCComponent.mTranform.position;
			TempHC = mPCComponent.ThrownHook.GetComponent<TempHookControl>();
			TempHC.ThrowVelocity = mPCComponent.ThrowHookVelocity;
			TempHC.rVelocity = TempHC.ThrowVelocity * mPCComponent.FacingDir * -1;
		}

		private void ThrowStartGoing()
		{
			//TODO(OriS):之后加一个缓速减速到0
			mPCComponent.CtrlVelocity = Vector2.zero;
		}

		private void ThrowMoveGoing()
		{
			mPCComponent.CtrlVelocity =
				new Vector2(ThrowMoveVelocity.x * mPCComponent.FacingDir, ThrowMoveVelocity.y);
		}

		private bool CanThrowCheck() => PreThrowInputTimer > 0
				&& PreThrowInputTimer < mPCComponent.PreThrowTime
				&& mPCComponent.IsCanThrow
				&& mPCComponent.ThrowCdInputTimer == 0;

		private TempHookControl TempHC;
		private bool IsHookThing;
	}
}
