using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public class PlayerGravityCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.Gravity;
            Tags = new List<EccTag> { EccTag.Gravity };
        }

        protected override bool OnShouldActivate()
        {
            return !mPlayerMovementComponent.IsGrounded();
        }

        protected override bool OnShouldDeactivate()
        {
            return mPlayerMovementComponent.IsGrounded();
        }

        protected override void OnTick(float deltaTime)
        {
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.y = Mathf.Max(
                velocity.y - (mPlayerMovementComponent.GravityAcceleration * deltaTime),
                -mPlayerMovementComponent.ClampVelocityY
            );

            mPlayerMovementComponent.Velocity = velocity;
        }
    }

    public class PlayerClampedFallSpeedCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.ClampedFallSpeed;
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected override bool OnShouldActivate()
        {
            return mPlayerMovementComponent.Velocity.y > mPlayerMovementComponent.ClampVelocityY;
        }

        protected override bool OnShouldDeactivate()
        {
            return mPlayerMovementComponent.Velocity.y <= mPlayerMovementComponent.ClampVelocityY;
        }

        protected override void OnTick(float deltaTime)
        {
            mPlayerMovementComponent.Velocity = new Vector2(
                mPlayerMovementComponent.Velocity.x,
                mPlayerMovementComponent.ClampVelocityY
            );
        }
    }
}
