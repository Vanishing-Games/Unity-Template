using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;
using Core;

namespace PlayerControlByOris
{
	public class PlayerControlGravity : PlayerControlCapabilityBase
	{
		protected override bool OnShouldActivate()
		{
			return !IsOnGround && !IsJumping;
		}

		protected override bool OnShouldDeactivate()
		{
			return !(!IsOnGround && !IsJumping);
		}

		protected override void OnTick(float deltaTime)
		{
			var velocity = mPCComponent.CtrlVelocity;
			float mult = (Mathf.Abs(velocity.y) < LowGravThresholdSpeedY
				&& mPCComponent.InputJump) ? LowGravMult : 1f;
			velocity.y = Approach(velocity.y, -1 * MaxFallSpeedY, mult * GravityAccY);
			mPCComponent.CtrlVelocity = velocity;
		}

		protected override void SetUpTickSettings()
		{
			base.SetUpTickSettings();
			TickOrderInGroup = (uint)PlayerControlTickOrder.GravityControl;
			Tags = new List<EccTag> { EccTag.NormalState };
		}
	}

	
}
