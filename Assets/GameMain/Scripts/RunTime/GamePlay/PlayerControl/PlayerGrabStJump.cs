using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerGrabStJump : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.GrabJumpControl;
            Tags = new List<EccTag> { EccTag.GrabState };
        }

        protected override bool ShouldActivate()
        {
            return mPCComponent.PreJumpInputTimer > 0
                && mPCComponent.PreJumpInputTimer < PreJumpInputTime;
        }

        protected override void OnActivate()
        {
            mPCComponent.IsJumping = true;
            mPCComponent.IsCornerGrab = false;
            SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
            Vector2 Velocity = mPCComponent.CtrlVelocity;
            Velocity.x = MoveX * MaxSpeedX;
            mPCComponent.CtrlVelocity = Velocity;
            mPCComponent.CanGrabCDTimer = mPCComponent.CanGrabCDTime;
        }

        protected override bool ShouldDeactivate()
        {
            return true;
        }

        protected override void OnTick(float deltaTime) { }
    }
}
