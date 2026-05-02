using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerInputTemp : EccCapability
    {
        protected override void SetUpTickSettings()
        {
            TickGroup = EccTickGroup.Input;
            TickType = EccTickType.ByFrame;
        }

        protected override void OnActivate() { }

        protected override void OnDeactivate() { }

        protected override void OnSetup()
        {
            mPCComponent = mOwner.GetEccComponent<PlayerControlComponent>();
        }

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
            mPCComponent.InputX = UnityEngine.Input.GetAxisRaw("Horizontal");
            mPCComponent.InputY = UnityEngine.Input.GetAxisRaw("Vertical");
            mPCComponent.InputJump = UnityEngine.Input.GetButton("Jump");
            mPCComponent.InputAct = UnityEngine.Input.GetButton("Act");
            mPCComponent.InputAct2 = UnityEngine.Input.GetButton("Act2");
        }

        protected PlayerControlComponent mPCComponent;
    }
}
