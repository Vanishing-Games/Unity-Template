using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerStateEndSet : PlayerControlCapabilityBase
    {
        protected override bool ShouldActivate()
        {
            return true;
        }

        protected override bool ShouldDeactivate()
        {
            return false;
        }

        protected override void OnTick(float deltaTime)
        {
            mPCComponent.mRigidbody.linearVelocity = ctrlVelocity + extraVelocity;
        }

        private Vector2 ctrlVelocity => mPCComponent.CtrlVelocity;
        private Vector2 extraVelocity => mPCComponent.ExtraVelocity;
    }
}
