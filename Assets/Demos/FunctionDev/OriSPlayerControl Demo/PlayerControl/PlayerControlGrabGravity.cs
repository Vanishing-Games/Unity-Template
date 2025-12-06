using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
	public class PlayerControlGrabGravity : PlayerControlCapabilityBase
	{
		protected override void SetUpTickSettings()
		{
			base.SetUpTickSettings();
			TickOrderInGroup = (uint)PlayerControlTickOrder.GrabGravity;
			Tags = new List<EccTag> { EccTag.GrabState };
		}

		protected override bool OnShouldActivate()
		{
			return !mPCComponent.IsCornerGrab && !mPCComponent.IsSafeGrab;
		}

		protected override void OnActivate()
		{
			//直接的接触绳子的暂留时间，之后会有不同情况无暂留
			mPCComponent.GrabStayRevTimer = mPCComponent.GrabStayTime;
		}

		protected override bool OnShouldDeactivate()
		{
			return !(!mPCComponent.IsCornerGrab && !mPCComponent.IsSafeGrab);
		}

		protected override void OnTick(float deltaTime)
		{
			if (mPCComponent.GrabStayRevTimer > 0)
			{
				mPCComponent.GrabStayRevTimer--;
			}
			else
			{
				var velocity = mPCComponent.CtrlVelocity;
				velocity.y = Approach(velocity.y, -1 * mPCComponent.SlideFallSpeedY, GravityAccY);
				mPCComponent.CtrlVelocity = velocity;
			}
		}
	}
}
