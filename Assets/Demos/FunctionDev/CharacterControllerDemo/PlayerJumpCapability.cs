using System;
using System.Collections.Generic;
using Core;
using R3;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public abstract class PlayerJumpCapabilityBase : PlayerMoveCapability
    {
        protected sealed override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.Jump;
            Tags = new List<EccTag> { EccTag.Jump };
        }

        protected sealed override bool OnShouldActivate()
        {
            return VgInput.GetButtonDownBuffered(InputAction.Jump) && ShouldActivateConditions();
        }

        protected abstract bool ShouldActivateConditions();

        protected sealed override bool OnShouldDeactivate()
        {
            return !VgInput.GetButton(InputAction.Jump)
                || mPlayerMovementComponent.IsUnderCeilingFast()
                || mPlayerMovementComponent.ReachesMaxJumpTime()
                || ShouldDeactivateConditions();
        }

        protected abstract bool ShouldDeactivateConditions();

        protected override void OnActivate()
        {
            base.OnActivate();
            mPlayerMovementComponent.JumpScaledTimeSinceStarted = 0f;
            mPlayerMovementComponent.CurrentMovementState = PlayerMoveState.Jumping;
            mOwner.BlockCapabilities(EccTag.Gravity, this);
            mPlayerMovementComponent.OnPlayerStartJumpEvent.OnNext(0);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            mPlayerMovementComponent.JumpScaledTimeSinceStarted = Mathf.Min(
                mPlayerMovementComponent.JumpScaledTimeSinceStarted,
                mPlayerMovementComponent.MaxJumpScaledTime
            );
            mPlayerMovementComponent.CurrentMovementState = PlayerMoveState.OnAir;
            mOwner.UnblockCapabilities(this);
            mPlayerMovementComponent.OnPlayerEndJumpEvent.OnNext(0);

            // reset jump time when landed
            Observable
                .EveryUpdate()
                .Where(_ => mPlayerMovementComponent.IsOnGroundFast())
                .Take(1)
                .Subscribe(_ =>
                {
                    mPlayerMovementComponent.JumpScaledTimeSinceStarted = 0f;
                });
        }

        protected sealed override void OnTick(float deltaTime)
        {
            mPlayerMovementComponent.JumpScaledTimeSinceStarted += deltaTime;

            var velocity = mPlayerMovementComponent.Velocity;

            velocity.y = mPlayerMovementComponent.JumpSpeedY;

            mPlayerMovementComponent.Velocity = velocity;
        }
    }

    public class PlayerJumpOnGroundCapability : PlayerJumpCapabilityBase
    {
        protected override bool ShouldActivateConditions()
        {
            return mPlayerMovementComponent.IsOnGroundFast();
        }

        protected override bool ShouldDeactivateConditions()
        {
            return false;
        }
    }

    public class PlayerCoyoteJumpCapability : PlayerJumpCapabilityBase
    {
        protected override bool ShouldActivateConditions()
        {
            return !mPlayerMovementComponent.IsJumping()
                && mPlayerMovementComponent.WithinCoyoteTime();
        }

        protected override bool ShouldDeactivateConditions()
        {
            return false;
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
            return VgInput.GetButton(InputAction.Jump)
                && !mPlayerMovementComponent.IsOnGroundFast()
                && !mPlayerMovementComponent.IsUnderCeilingFast()
                && mPlayerMovementComponent.WithinJumpApexModifierTime();
        }

        protected override bool OnShouldDeactivate()
        {
            return !VgInput.GetButton(InputAction.Jump)
                || mPlayerMovementComponent.IsOnGroundFast()
                || mPlayerMovementComponent.IsUnderCeilingFast()
                || !mPlayerMovementComponent.WithinJumpApexModifierTime();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            mOwner.BlockCapabilities(EccTag.Gravity, this);

            mPlayerMovementComponent.CurrentMovementState = PlayerMoveState.Apex;

            mPlayerMovementComponent.MaxVelocityX +=
                mPlayerMovementComponent.JumpApexSpeedLimitModifier;

            mPlayerMovementComponent.JumpApexModifierScaledTimeSinceStarted = 0f;

            mPlayerMovementComponent.OnPlayerEnterJumpApexEvent.OnNext(0);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            mOwner.UnblockCapabilities(this);

            mPlayerMovementComponent.CurrentMovementState = PlayerMoveState.OnAir;

            mPlayerMovementComponent.MaxVelocityX -=
                mPlayerMovementComponent.JumpApexSpeedLimitModifier;

            mPlayerMovementComponent.OnPlayerLeaveJumpApexEvent.OnNext(0);

            Observable
                .EveryUpdate()
                .Where(_ => mPlayerMovementComponent.IsOnGroundFast())
                .Take(1)
                .Subscribe(_ =>
                {
                    mPlayerMovementComponent.JumpApexModifierScaledTimeSinceStarted = 0f;
                });
        }

        protected override void OnTick(float deltaTime)
        {
            SimulateGravityEffect(deltaTime);
        }

        private void SimulateGravityEffect(float deltaTime)
        {
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.y = Mathf.Max(
                velocity.y - (mPlayerMovementComponent.JumpApexGravityValue * deltaTime),
                -mPlayerMovementComponent.ClampVelocityY
            );

            mPlayerMovementComponent.Velocity = velocity;
        }
    }

    public class PlayerJumpExtraSpeedCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.JumpExtraSpeed;
            Tags = new List<EccTag> { EccTag.Jump };
        }

        protected override void OnSetup()
        {
            base.OnSetup();

            mPlayerMovementComponent.OnPlayerStartJumpEvent.Subscribe(_ =>
            {
                var velocity = mPlayerMovementComponent.Velocity;

                velocity.x +=
                    mPlayerMovementComponent.JumpExtraSpeedX
                    * Math.Sign(VgInput.GetAxis(InputAxis.LeftStickHorizontal));

                mPlayerMovementComponent.Velocity = velocity;
            });
        }

        protected override bool OnShouldActivate()
        {
            return false;
        }

        protected override bool OnShouldDeactivate()
        {
            return true;
        }

        protected override void OnTick(float deltaTime)
        {
            return;
        }
    }
}
