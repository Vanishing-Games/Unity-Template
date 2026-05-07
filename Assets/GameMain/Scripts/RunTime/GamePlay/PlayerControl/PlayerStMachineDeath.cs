using System.Collections;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
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

        protected override bool ShouldActivate()
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

            //VgLoadingSplashManager.Instance.CoverAsync(VgSplashKey.Default).Forget();

            //状态变量修正
            mPCComponent.IsOnGround = false;
            mPCComponent.IsJumping = false;
            mPCComponent.IsByWallLeft = false;
            mPCComponent.IsByWallRight = false;
            mPCComponent.IsSafeGrab = false;
            mPCComponent.IsCornerGrab = false;
        }

        protected override void OnDeactivate()
        {
            mPCComponent.isShouldDie = false;
            //VgLoadingSplashManager.Instance.RevealAsync(VgSplashKey.Default).Forget();
        }

        protected override bool ShouldDeactivate()
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
                if (mPCComponent.DeathTimer == mPCComponent.DeathTime)
                {
                    VgLoadingSplashManager.Instance.CoverAsync(VgSplashKey.Default).Forget();
                    EffectPoolManager.Instance.SpawnEffect(
                        "brustOut",
                        mPCComponent.mTranform.position,
                        1
                    );
                }

                mPCComponent.DeathTimer--;
            }

            if (mPCComponent.DeathTimer == 0 && mPCComponent.RespawnTimer > 0)
            {
                //之后改为执行一次
                if (mPCComponent.RespawnTimer == mPCComponent.RespawnTime)
                {
                    VgLoadingSplashManager.Instance.RevealAsync(VgSplashKey.Default).Forget();

                    //发布的时间节点要思考一下
                    MessageBroker.Global.Publish(new GamePlayMatEvents.MatPlayerDeathEvent());

                    mPCComponent.mTranform.position =
                        mPCComponent.RespawnPos + mPCComponent.RespawnOffset;

                    EffectPoolManager.Instance.SpawnEffect(
                        "suckIn",
                        mPCComponent.mTranform.position,
                        1
                    );
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
