using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;
using Core;

namespace Oris1
{
	public class HelloWorldCapability : EccCapability
	{
		protected override void OnActivate()
		{
			
		}

		protected override void OnDeactivate()
		{
			throw new System.NotImplementedException();
		}

		protected override void OnSetup()
		{
			
		}

		protected override bool OnShouldActivate()
		{
			return true;
		}

		protected override bool OnShouldDeactivate()
		{
			throw new System.NotImplementedException();
		}

		protected override void OnTick(float deltaTime)
		{
			Core.Logger.LogInfo("Hello World!", LogTag.CoreModule);
		}

		protected override void SetUpTickSettings()
		{
			
		}
	}
}
