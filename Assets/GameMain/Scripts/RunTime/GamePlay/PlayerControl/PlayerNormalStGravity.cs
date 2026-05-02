using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerNormalStGravity : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.GravityControl;
            Tags = new List<EccTag> { EccTag.NormalState };
        }

        protected override bool ShouldActivate()
        {
            return !IsOnGround && !IsJumping;
        }

        protected override bool ShouldDeactivate()
        {
            return !(!IsOnGround && !IsJumping);
        }

        protected override void OnTick(float deltaTime)
        {
            var velocity = mPCComponent.CtrlVelocity;
            //滑墙判断
            if (WallSlideCheck())
            {
                velocity.y = Approach(velocity.y, -1 * mPCComponent.SlideFallSpeedY, GravityAccY);
            }
            else
            {
                float mult = LowGravMultCheck(velocity);
                velocity.y = Approach(velocity.y, -1 * MaxFallSpeedY, mult * GravityAccY);
            }
            mPCComponent.CtrlVelocity = velocity;
        }

        private float LowGravMultCheck(Vector2 velocity) =>
            (Mathf.Abs(velocity.y) < LowGravThresholdSpeedY && mPCComponent.InputJump)
                ? LowGravMult
                : 1f;

        public bool WallSlideCheck() =>
            (mPCComponent.LeftSlideCheck && mPCComponent.InputX < 0)
            || (mPCComponent.RightSlideCheck && mPCComponent.InputX > 0);
    }
}
