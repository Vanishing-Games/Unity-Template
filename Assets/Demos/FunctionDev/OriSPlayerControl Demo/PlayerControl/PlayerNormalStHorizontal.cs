using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
    public class PlayerNormalStHorizontal : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.HorizontalControl;
            Tags = new List<EccTag> { EccTag.NormalState };
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
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
            var velocity = mPCComponent.CtrlVelocity;
            float mult = 1;
            if (!mPCComponent.IsOnGround)
                mult = AirAccMultX;

            if (Mathf.Abs(velocity.x) > MaxSpeedX && Math.Sign(velocity.x) == MoveX)
                velocity.x = Approach(velocity.x, MoveX * MaxSpeedX, OverReduceX * mult);
            else
                velocity.x = Approach(velocity.x, MoveX * MaxSpeedX, AccX * mult);

            mPCComponent.CtrlVelocity = velocity;
        }
    }
}
