using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public abstract class PlayerAccelerateCapability : PlayerMoveCapability
    {
        protected sealed override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = GetTickOrderInGroup();
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected sealed override bool OnShouldActivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) == 0)
                return false;

            if (!mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return false;

            if (!ShouldActivateConditions())
                return false;

            return true;
        }

        protected sealed override bool OnShouldDeactivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) == 0)
                return true;

            if (!mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return true;

            if (ShouldDeactivateConditions())
                return true;

            return false;
        }

        protected sealed override void OnTick(float deltaTime)
        {
            float inputX = VgInput.GetAxis(InputAxis.LeftStickHorizontal);
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.x = Mathf.Clamp(
                velocity.x + (inputX * GetAcceleration() * deltaTime),
                -mPlayerMovementComponent.MaxVelocityX,
                mPlayerMovementComponent.MaxVelocityX
            );

            mPlayerMovementComponent.Velocity = velocity;
        }

        protected abstract bool ShouldActivateConditions();

        protected abstract bool ShouldDeactivateConditions();

        protected abstract float GetAcceleration();

        protected abstract uint GetTickOrderInGroup();
    }

    public class PlayerAccelerateOnGroundCapability : PlayerAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() => PlayerMovementTickOrder.AccelerateOnGround;

        protected override float GetAcceleration() => mPlayerMovementComponent.AccelerationOnGround;

        protected override bool ShouldActivateConditions() =>
            mPlayerMovementComponent.IsGrounded() && !mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() || mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerAccelerateOnAirCapability : PlayerAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() => PlayerMovementTickOrder.AccelerateOnAir;

        protected override float GetAcceleration() => mPlayerMovementComponent.AccelerationOnAir;

        protected override bool ShouldActivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() && !mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            mPlayerMovementComponent.IsGrounded() || mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerAccelerateWhileOverspeedOnGroundCapability : PlayerAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            PlayerMovementTickOrder.AccelerateWhileOverspeedOnGround;

        protected override float GetAcceleration() =>
            mPlayerMovementComponent.AccelerationWhileOverspeedOnGround;

        protected override bool ShouldActivateConditions() =>
            mPlayerMovementComponent.IsGrounded() && mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() || !mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerAccelerateWhileOverspeedOnAirCapability : PlayerAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            PlayerMovementTickOrder.AccelerateWhileOverspeedOnAir;

        protected override float GetAcceleration() =>
            mPlayerMovementComponent.AccelerationWhileOverspeedOnAir;

        protected override bool ShouldActivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() && mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            mPlayerMovementComponent.IsGrounded() || !mPlayerMovementComponent.IsOverSpeed();
    }
}
