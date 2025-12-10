using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;
using Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using R3;

namespace PlayerControlByOris
{
	public partial class PlayerControlComponent : EccComponent
	{
		[HideInInspector]
		public Subject<EccTag> TagChangeEvent = new Subject<EccTag>();
	}
}
