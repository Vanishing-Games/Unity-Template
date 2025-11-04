using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public abstract class PlayerDeAccelerateCapability : PlayerMoveCapability
    {
        protected sealed override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = GetTickOrderInGroup();
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected sealed override bool OnShouldActivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) != 0)
                return false;

            if (mPlayerMovementComponent.Velocity.x == 0)
                return false;

            if (!ShouldActivateConditions())
                return false;

            return true;
        }

        protected sealed override bool OnShouldDeactivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) != 0)
                return true;

            if (mPlayerMovementComponent.Velocity.x == 0)
                return true;

            if (ShouldDeactivateConditions())
                return true;

            return false;
        }

        protected sealed override void OnTick(float deltaTime)
        {
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.x = Mathf.MoveTowards(velocity.x, 0f, GetDeAcceleration() * deltaTime);

            mPlayerMovementComponent.Velocity = velocity;
        }

        protected abstract bool ShouldActivateConditions();

        protected abstract bool ShouldDeactivateConditions();

        protected abstract float GetDeAcceleration();

        protected abstract uint GetTickOrderInGroup();
    }

    public class PlayerDeAccelerateOnGroundCapability : PlayerDeAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            (uint)PlayerMovementTickOrder.DeAccelerateOnGround;

        protected override float GetDeAcceleration() =>
            mPlayerMovementComponent.DeAccelerationOnGround;

        protected override bool ShouldActivateConditions() =>
            mPlayerMovementComponent.IsGrounded() && !mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() || mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerDeAccelerateOnAirCapability : PlayerDeAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            (uint)PlayerMovementTickOrder.DeAccelerateOnAir;

        protected override float GetDeAcceleration() =>
            mPlayerMovementComponent.DeAccelerationOnAir;

        protected override bool ShouldActivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() && !mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            mPlayerMovementComponent.IsGrounded() || mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerDeAccelerateWhileOverspeedOnGroundCapability : PlayerDeAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            (uint)PlayerMovementTickOrder.DeAccelerateWhileOverspeedOnGround;

        protected override float GetDeAcceleration() =>
            mPlayerMovementComponent.DeAccelerationWhileOverspeedOnGround;

        protected override bool ShouldActivateConditions() =>
            mPlayerMovementComponent.IsGrounded() && mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() || !mPlayerMovementComponent.IsOverSpeed();
    }

    public class PlayerDeAccelerateWhileOverspeedOnAirCapability : PlayerDeAccelerateCapability
    {
        protected override uint GetTickOrderInGroup() =>
            (uint)PlayerMovementTickOrder.DeAccelerateWhileOverspeedOnAir;

        protected override float GetDeAcceleration() =>
            mPlayerMovementComponent.DeAccelerationWhileOverspeedOnAir;

        protected override bool ShouldActivateConditions() =>
            !mPlayerMovementComponent.IsGrounded() && mPlayerMovementComponent.IsOverSpeed();

        protected override bool ShouldDeactivateConditions() =>
            mPlayerMovementComponent.IsGrounded() || !mPlayerMovementComponent.IsOverSpeed();
    }
}
