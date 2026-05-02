using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerNormalStJump : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.JumpControl;
            Tags = new List<EccTag> { EccTag.NormalState };
        }

        protected override void OnActivate()
        {
            mPCComponent.JumpingTimer = 0;
            mPCComponent.IsJumping = true;
            mPCComponent.IsOnGround = false;

            Vector2 velocity = mPCComponent.CtrlVelocity;
            velocity.x += MoveX * JumpBoostSpeedX;
            mPCComponent.CtrlVelocity = velocity;
            mPCComponent.CoyoteJumpInputRevTimer = 0;

            MessageBroker.Global.Publish(new PlayerControlEvents.PlayerStartJumpEvent());
        }

        protected override void OnDeactivate()
        {
            mPCComponent.JumpingTimer = 0;
            mPCComponent.IsJumping = false;
        }

        protected override bool ShouldActivate()
        {
            return IsReadyJump();
        }

        protected override bool ShouldDeactivate()
        {
            return IsEndJump();
        }

        protected override void OnTick(float deltaTime)
        {
            Vector2 velocity = mPCComponent.CtrlVelocity;
            velocity.y = JumpSpeedY;
            mPCComponent.JumpingTimer++;
            mPCComponent.CtrlVelocity = velocity;
        }

        protected bool IsReadyJump()
        {
            if (GroundJumpCheck())
                return true;
            else if (CoyoteJumpCheck())
                return true;
            else if (WallJumpCheck())
            {
                //强制转向
                mPCComponent.ForceMoveXRevTimer = mPCComponent.WallJumpForceTime;
                if (mPCComponent.IsByWallLeft)
                    mPCComponent.ForceMoveX = 1;
                else if (mPCComponent.IsByWallRight)
                    mPCComponent.ForceMoveX = -1;
                else
                    mPCComponent.ForceMoveX = mPCComponent.WallDir;
                mPCComponent.MoveX = mPCComponent.ForceMoveX;
                //给予反墙初始速度
                Vector2 velocity = mPCComponent.CtrlVelocity;
                velocity.x = mPCComponent.ForceMoveX * mPCComponent.MaxSpeedX;
                mPCComponent.CtrlVelocity = velocity;
                return true;
            }
            else if (IsJumping)
                return true;
            else
                return false;
        }

        protected bool IsEndJump()
        {
            if (mPCComponent.JumpingTimer > MinJumpTime && !InputJump)
                return true;
            else if (mPCComponent.JumpingTimer >= MaxJumpTime)
                return true;
            else if (CollisionEndJump())
                return true;
            else
                return false;
        }

        protected bool CollisionEndJump() => !IsJumping;

        protected bool GroundJumpCheck() =>
            IsOnGround
            && mPCComponent.PreJumpInputTimer > 0
            && mPCComponent.PreJumpInputTimer < PreJumpInputTime;

        protected bool CoyoteJumpCheck() =>
            !IsOnGround
            && mPCComponent.PreJumpInputTimer > 0
            && mPCComponent.PreJumpInputTimer < PreJumpInputTime
            && mPCComponent.CoyoteJumpInputRevTimer > 0;

        protected bool WallJumpCheck() =>
            !IsOnGround
            && mPCComponent.PreJumpInputTimer > 0
            && mPCComponent.PreJumpInputTimer < PreJumpInputTime
            && (
                (mPCComponent.IsByWallLeft || mPCComponent.IsByWallRight)
                || mPCComponent.WallCoyoteJumpInputRevTimer > 0
            )
            && !mPCComponent.IsCornerGrab
            && mPCComponent.CtrlVelocity.y < 0;
    }
}
