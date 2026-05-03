using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerInitialSet : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.InitialSet;
        }

        protected override void OnSetup()
        {
            base.OnSetup();
            mPCComponent.FacingDir = 1;
            StateTag = this;
            SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
        }

        protected override bool ShouldActivate()
        {
            return true;
        }

        protected override bool ShouldDeactivate()
        {
            return false;
        }

        protected override void OnTick(float deltaTime) { }
    }
}
