using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace Oris1
{
    public partial class PlayerMovementComponent : EccComponent
    {

		protected override void OnSetup()
		{
			mRigidbody = mGameObject.GetComponent<Rigidbody2D>();
		}

		protected override void OnUpdateGo(float deltaTime)
		{
			mRigidbody.velocity = Velocity;
		}

		private Rigidbody2D mRigidbody;
		
	}
}
