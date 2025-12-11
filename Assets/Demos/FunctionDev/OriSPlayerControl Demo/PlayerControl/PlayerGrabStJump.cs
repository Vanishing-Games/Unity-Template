using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
    public class PlayerGrabStJump : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.GrabJumpControl;
            Tags = new List<EccTag> { EccTag.GrabState };
        }

        protected override bool OnShouldActivate()
        {
            return mPCComponent.PreJumpInputTimer > 0
                && mPCComponent.PreJumpInputTimer < PreJumpInputTime;
        }

        protected override void OnActivate()
        {
            mPCComponent.IsJumping = true;
            SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
            Vector2 Velocity = mPCComponent.CtrlVelocity;
            Velocity.x = MoveX * MaxSpeedX;
            mPCComponent.CtrlVelocity = Velocity;
        }

        protected override bool OnShouldDeactivate()
        {
            return true;
        }

        protected override void OnTick(float deltaTime) { }
    }
}
