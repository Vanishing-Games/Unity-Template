using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;
using Core;

namespace PlayerControlByOris
{
	public class PlayerStateEndSet : PlayerControlCapabilityBase
	{
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
			mPCComponent.mRigidbody.velocity = ctrlVelocity + extraVelocity;
		}

		private Vector2 ctrlVelocity => mPCComponent.CtrlVelocity;
		private Vector2 extraVelocity => mPCComponent.ExtraVelocity;
	}
}
