using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerStMachineThrow : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.ThrowControl;
        }

        protected override bool ShouldActivate()
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

        protected override bool ShouldDeactivate()
        {
            return ThrowMoveTimer == 0
                || (BeeToThrow.currentState == BeeState.FollowSt && ThrowStartTimer == 0);
        }

        protected override void OnDeactivate()
        {
            if (BeeToThrow.currentState == BeeState.StaySt)
            {
                SetStateMachine(PlayerStateMachine.DashState, EccTag.DashState);
            }
            else
            {
                SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
                BeeToThrow.ChangeState(BeeState.FollowSt);
            }
        }

        protected override void OnTick(float deltaTime)
        {
            if (ThrowStartTimer > 0)
            {
                mPCComponent.ThrowStartTimer--;
                ThrowStartGoing();
                BeeToThrow.FlashToPosition(ThrowStartPosition, true);
            }

            if (ThrowStartTimer == 0 && ThrowMoveTimer == mPCComponent.ThrowMoveTime)
            {
                BeeToThrow.BeeThrow(
                    mPCComponent.ThrowHookVelocity * mPCComponent.FacingDir * -1,
                    mPCComponent.FacingDir < 0
                );
            }

            if (ThrowStartTimer == 0 && ThrowMoveTimer > 0)
            {
                mPCComponent.ThrowMoveTimer--;
                ThrowMoveGoing();
            }
        }

        //飞虫投掷
        private void BeeThrow() { }

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
            mPCComponent.CtrlVelocity = new Vector2(
                ThrowMoveVelocity.x * mPCComponent.FacingDir,
                ThrowMoveVelocity.y
            );
        }

        private bool CanThrowCheck() =>
            PreThrowInputTimer > 0
            && PreThrowInputTimer < mPCComponent.PreThrowTime
            && mPCComponent.IsCanThrow
            && mPCComponent.ThrowCdInputTimer == 0;

        private BeeMainControl BeeToThrow => mPCComponent.BeeToThrow;
        private Vector3 ThrowStartPosition =>
            mPCComponent.mTranform.position - mPCComponent.ThrowStartOffset;
        private TempHookControl TempHC;
        private bool IsHookThing;
    }
}
