using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using R3.Triggers;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public partial class PlayerControlComponent : EccComponent
    {
        private void AnimControl()
        {
            mTranform.localScale = new Vector3(FacingDir, 1, 1);

            mAnim.SetBool("IsOnGround", IsOnGround);
            mAnim.SetBool("IsMove", MoveX != 0);
            mAnim.SetFloat("SpeedY", CtrlVelocity.y);
            mAnim.SetFloat("SpeedX", MathF.Abs(CtrlVelocity.x));

            if (MathF.Abs(CtrlVelocity.x) <= 0.5f * MaxSpeedX)
                mAnim.SetBool("HorizontalFast", false);
            else if (MoveX * CtrlVelocity.x < -0.5f * MaxSpeedX)
                mAnim.SetBool("HorizontalFast", false);
            else
                mAnim.SetBool("HorizontalFast", true);

            if (MoveX * CtrlVelocity.x < -0.5f * MaxSpeedX)
                mAnim.SetBool("MoveBack", true);
            else
                mAnim.SetBool("MoveBack", false);

            mAnim.SetBool("IsGrab", CurrentState == PlayerStateMachine.GrabState);
            mAnim.SetBool("IsCorner", IsCornerGrab);

            mAnim.SetBool("ThrowBegin", ThrowStartTimer > 0);

            mAnim.SetBool("Throwing", ThrowMoveTimer > 0 && ThrowStartTimer == 0);

            mAnim.SetBool("DashBefore", DashWaitTimer > 0);

            mAnim.SetBool("Dashing", DashTimer > 0 && DashWaitTimer == 0);

            mAnim.SetBool("WhistleBefore", WhistleBeforeTimer > 0);

            mAnim.SetBool("WhistleStay", WhistleBeforeTimer == 0 && WhistleStayTimer > 0);

            mAnim.SetBool(
                "WhistleAfter",
                WhistleStayTimer == 0 && WhistleAfterTimer > 0 && IsInputEnd
            );

            mAnim.SetBool("IsSlide", WallSlideCheck());

            mAnim.SetBool("IsNormalSt", CurrentState == PlayerStateMachine.NormalState);

            mAnim.SetBool("IsDeath", CurrentState == PlayerStateMachine.DeathState);
            mAnim.SetBool("IsDeathBefore", DyingBeforeTimer > 0);
            mAnim.SetBool("IsDeathDying", DyingTimer > 0 && DyingBeforeTimer == 0);
            mAnim.SetBool("IsDeathRespawn", DeathTimer == 0 && RespawnTimer > 0);
        }

        public bool WallSlideCheck() =>
            (LeftSlideCheck && InputX < 0) || (RightSlideCheck && InputX > 0);
    }
}
