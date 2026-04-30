using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerStMachineDeath : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.DeathControl;
        }

        protected override void OnSetup()
        {
            base.OnSetup();
        }

        protected override bool OnShouldActivate()
        {
            return isDeath();
        }

        protected override void OnActivate()
        {
            SetStateMachine(PlayerStateMachine.DeathState, EccTag.DeathState);
            mPCComponent.CtrlVelocity = Vector2.zero;
            mPCComponent.DyingBeforeTimer = mPCComponent.DyingBeforeTime;
            mPCComponent.DyingTimer = mPCComponent.DyingTime;
            mPCComponent.DeathTimer = mPCComponent.DeathTime;
            mPCComponent.RespawnTimer = mPCComponent.RespawnTime;
        }

        protected override void OnDeactivate()
        {
            mPCComponent.isShouldDie = false;
        }

        protected override bool OnShouldDeactivate()
        {
            return DeathEnd();
        }

        protected override void OnTick(float deltaTime)
        {
            if (mPCComponent.DyingBeforeTimer > 0)
            {
                mPCComponent.DyingBeforeTimer--;
                mPCComponent.CtrlVelocity =
                    mPCComponent.DyingBackVelocity * new Vector2(mPCComponent.FacingDir, 1f);
            }

            if (mPCComponent.DyingTimer > 0 && mPCComponent.DyingBeforeTimer == 0)
            {
                mPCComponent.CtrlVelocity = Vector2.zero;
                mPCComponent.DyingTimer--;
            }

            if (mPCComponent.DeathTimer > 0 && mPCComponent.DyingTimer == 0)
            {
                mPCComponent.DeathTimer--;
                if (mPCComponent.RespawnBlackMask != null)
                    mPCComponent.RespawnBlackMask.SetActive(false);
            }

            if (mPCComponent.DeathTimer == 0 && mPCComponent.RespawnTimer > 0)
            {
                //之后改为执行一次
                if (mPCComponent.RespawnTimer == mPCComponent.RespawnTime)
                {
                    if (mPCComponent.RespawnBlackMask != null)
                        mPCComponent.RespawnBlackMask.SetActive(true);

                    //发布的时间节点要思考一下
                    MessageBroker.Global.Publish(new GamePlayMatEvents.MatPlayerDeathEvent());

                    mPCComponent.mTranform.position =
                        mPCComponent.RespawnPos + mPCComponent.RespawnOffset;
                }

                mPCComponent.RespawnTimer--;
            }

            if (mPCComponent.RespawnTimer == 0)
            {
                SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
            }
        }

        private bool isDeath() => mPCComponent.isShouldDie;

        private bool DeathEnd() => mPCComponent.CurrentState != PlayerStateMachine.DeathState;
    }
}
