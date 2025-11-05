using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public class PlayerJumpCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.Jump;
            Tags = new List<EccTag> { EccTag.Jump };
        }

        protected override bool OnShouldActivate()
        {
            return VgInput.GetButton(InputAction.Jump) && mPlayerMovementComponent.IsGrounded();
        }

        protected override bool OnShouldDeactivate()
        {
            return !VgInput.GetButton(InputAction.Jump)
                || mPlayerMovementComponent.JumpPressedScaledTime
                    >= mPlayerMovementComponent.MaxJumpScaledTime;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            mPlayerMovementComponent.JumpPressedScaledTime = 0f;
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            mPlayerMovementComponent.JumpPressedScaledTime = 0f;
        }

        protected override void OnTick(float deltaTime)
        {
            mPlayerMovementComponent.JumpPressedScaledTime += deltaTime;

            var velocity = mPlayerMovementComponent.Velocity;

            velocity.y = mPlayerMovementComponent.JumpSpeedY;

            mPlayerMovementComponent.Velocity = velocity;
        }
    }

    public class PlayerJumpApexModifiersCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.JumpApexModifiers;
            Tags = new List<EccTag> { EccTag.Jump };
        }

        protected override bool OnShouldActivate()
        {
            return false;
        }

        protected override bool OnShouldDeactivate()
        {
            return true;
        }

        protected override void OnTick(float deltaTime) { }
    }

    public class PlayerJumpBufferingCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.JumpBuffering;
            Tags = new List<EccTag> { EccTag.Jump };
        }

        protected override bool OnShouldActivate()
        {
            return false;
        }

        protected override bool OnShouldDeactivate()
        {
            return true;
        }

        protected override void OnTick(float deltaTime) { }
    }

    public class PlayerCoyoteTimeCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.CoyoteTime;
            Tags = new List<EccTag> { EccTag.Jump };
        }

        protected override bool OnShouldActivate()
        {
            return false;
        }

        protected override bool OnShouldDeactivate()
        {
            return true;
        }

        protected override void OnTick(float deltaTime) { }
    }

    public class PlayerJumpExtraSpeedCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.JumpExtraSpeed;
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected override bool OnShouldActivate()
        {
            return false;
        }

        protected override bool OnShouldDeactivate()
        {
            return true;
        }

        protected override void OnTick(float deltaTime) { }
    }
}
