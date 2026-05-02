using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            VgLoadingSplashManager.Instance.CoverAsync(VgSplashKey.Default).Forget();
            SetStateMachine(PlayerStateMachine.DeathState, EccTag.DeathState);
            mPCComponent.CtrlVelocity = Vector2.zero;
            mPCComponent.DyingTimer = mPCComponent.DyingTime;
            mPCComponent.DeathTimer = mPCComponent.DeathTime;
            mPCComponent.RespawnTimer = mPCComponent.RespawnTime;
        }

        protected override void OnDeactivate()
        {
            VgLoadingSplashManager.Instance.RevealAsync(VgSplashKey.Default).Forget();
            mPCComponent.isShouldDie = false;
        }

        protected override bool ShouldDeactivate()
        {
            return DeathEnd();
        }

        protected override void OnTick(float deltaTime)
        {
            if (mPCComponent.DyingTimer > 0)
                mPCComponent.DyingTimer--;

            if (mPCComponent.DeathTimer > 0 && mPCComponent.DyingTimer == 0)
            {
                mPCComponent.DeathTimer--;
                if (mPCComponent.RespawnBlackMask != null)
                    mPCComponent.RespawnBlackMask.SetActive(false);
            }

            if (mPCComponent.DeathTimer == 0 && mPCComponent.RespawnTimer > 0)
            {
                mPCComponent.RespawnTimer--;
                //之后改为执行一次
                mPCComponent.mTranform.position = mPCComponent.RespawnPos;
                mPCComponent.BeeToThrow.ChangeState(BeeState.FollowSt);
            }

            if (mPCComponent.RespawnTimer == 0)
            {
                SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
                if (mPCComponent.RespawnBlackMask != null)
                    mPCComponent.RespawnBlackMask.SetActive(true);
            }
        }

        private bool isDeath() => mPCComponent.isShouldDie;

        private bool DeathEnd() => mPCComponent.CurrentState != PlayerStateMachine.DeathState;
    }
}
