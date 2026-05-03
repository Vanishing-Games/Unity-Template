using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerStMachineDash : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.DashControl;
        }

        protected override bool ShouldActivate()
        {
            return mPCComponent.CurrentState == PlayerStateMachine.DashState;
        }

        protected override void OnActivate()
        {
            DashDir = (mPCComponent.BeeToThrow.transform.position - DashPlayerOffsetPos).normalized;
            mPCComponent.DashTimer = mPCComponent.DashTime;
            mPCComponent.DashWaitTimer = mPCComponent.DashWaitTime;
        }

        protected override bool ShouldDeactivate()
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

            //Wait计时结束后开始冲刺
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
                        mPCComponent.DashTimer
                    );
                }
                mPCComponent.DashTimer--;
            }
        }

        private Vector3 DashPlayerOffsetPos =>
            mPCComponent.mTranform.position - mPCComponent.ThrowStartOffset;
        private Vector2 DashDir;
    }
}
