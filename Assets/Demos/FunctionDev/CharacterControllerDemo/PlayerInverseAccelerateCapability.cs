using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public abstract class PlayerInverseAccelerateCapability : PlayerMoveCapability
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

            if (mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return false;

            if (!ShouldActivateConditions())
                return false;

            return true;
        }

        protected sealed override bool OnShouldDeactivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) == 0)
                return true;

            if (mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return true;

            if (ShouldDeactivateConditions())
                return true;

            return false;
        }

        protected sealed override void OnTick(float deltaTime)
        {
            float inputX = Mathf.Abs(VgInput.GetAxis(InputAxis.LeftStickHorizontal));
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                0,
                inputX * GetInverseAcceleration() * deltaTime
            );

            mPlayerMovementComponent.Velocity = velocity;
        }

        protected abstract bool ShouldActivateConditions();

        protected abstract bool ShouldDeactivateConditions();

        protected abstract float GetInverseAcceleration();

        protected abstract uint GetTickOrderInGroup();
    }

    public class PlayerInverseAccelerateOnGroundCapability : PlayerInverseAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            PlayerMovementTickOrder.InverseAccelerateOnGround;

        protected override float GetInverseAcceleration() =>
            mPlayerMovementComponent.InverseAccelerationOnGround;

        protected override bool ShouldActivateConditions() =>
            mPlayerMovementComponent.IsGrounded() && !mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() || mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerInverseAccelerateOnAirCapability : PlayerInverseAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            PlayerMovementTickOrder.InverseAccelerateOnAir;

        protected override float GetInverseAcceleration() =>
            mPlayerMovementComponent.InverseAccelerationOnAir;

        protected override bool ShouldActivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() && !mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            mPlayerMovementComponent.IsGrounded() || mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerInverseAccelerateWhileOverspeedOnGroundCapability
        : PlayerInverseAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            PlayerMovementTickOrder.InverseAccelerateWhileOverspeedOnGround;

        protected override float GetInverseAcceleration() =>
            mPlayerMovementComponent.InverseAccelerationWhileOverspeedOnGround;

        protected override bool ShouldActivateConditions() =>
            mPlayerMovementComponent.IsGrounded() && mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() || !mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerInverseAccelerateWhileOverspeedOnAirCapability
        : PlayerInverseAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            PlayerMovementTickOrder.InverseAccelerateWhileOverspeedOnAir;

        protected override float GetInverseAcceleration() =>
            mPlayerMovementComponent.InverseAccelerationWhileOverspeedOnAir;

        protected override bool ShouldActivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() && mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            mPlayerMovementComponent.IsGrounded() || !mPlayerMovementComponent.IsOverSpeed();
    }
}
