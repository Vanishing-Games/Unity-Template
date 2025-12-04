using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using R3.Triggers;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
	public partial class PlayerControlComponent : EccComponent
	{
        private void AnimControl()
		{
			mTranform.localScale = new Vector3(FacingDir, 1, 1);

			mAnim.SetBool("IsOnGround", IsOnGround);
			mAnim.SetBool("IsMove", MoveX != 0);
			mAnim.SetFloat("SpeedY", CtrlVelocity.y);
			mAnim.SetFloat("SpeedX", CtrlVelocity.x);

			if (MathF.Abs(CtrlVelocity.x) <= 0.5f * MaxSpeedX)
				mAnim.SetBool("HorizontalFast", false);
			else if (MoveX * CtrlVelocity.x < -0.5f * MaxSpeedX)
				mAnim.SetBool("HorizontalFast", false);
			else
				mAnim.SetBool("HorizontalFast", true);

		}
    }
}
