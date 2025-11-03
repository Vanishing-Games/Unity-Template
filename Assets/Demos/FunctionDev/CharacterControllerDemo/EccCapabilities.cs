using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    // csharpier-ignore-start
    public static class PlayerMovementTiceOrder
    {
        public const int AccelerateOnGround                      = 0;
        public const int InverseAccelerateOnGround               = 1;
        public const int DeAccelerateOnGround                    = 2;
        public const int AccelerateOnAir                         = 3;
        public const int InverseAccelerateOnAir                  = 4;
        public const int DeAccelerateOnAir                       = 5;
        public const int AccelerateWhileOverspeedOnGround        = 6;
        public const int InverseAccelerateWhileOverspeedOnGround = 7;
        public const int DeAccelerateWhileOverspeedOnGround      = 8;
        public const int AccelerateWhileOverspeedOnAir           = 9;
        public const int InverseAccelerateWhileOverspeedOnAir    = 10;
        public const int DeAccelerateWhileOverspeedOnAir         = 11;
        public const int Gravity                                 = 12;
        public const int Jump                                    = 13;
        public const int JumpApexModifiers                       = 14;
        public const int JumpBuffering                           = 15;
        public const int CoyoteTime                              = 16;
        public const int ClampedFallSpeed                        = 17;
        public const int EdgeSliding                             = 18;
        public const int JumpExtraSpeed                          = 19;
    }
    // csharpier-ignore-end

    public abstract class PlayerMoveCapability : EccCapability
    {
        protected override void OnSetup()
        {
            mPlayerMovementComponent = mOwner.GetEccComponent<PlayerMovementComponent>();
        }

        protected override void SetUpTickSettings()
        {
            TickGroup = EccTickGroup.Movement;
        }

        protected override void OnActivate() { }

        protected override void OnDeactivate() { }

        protected PlayerMovementComponent mPlayerMovementComponent;
    }

    public class PlayerAccelerateOnGroundCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.AccelerateOnGround;
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected override bool OnShouldActivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) == 0)
                return false;

            if (!mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return false;

            if (!mPlayerMovementComponent.IsGrounded())
                return false;

            return true;
        }

        protected override bool OnShouldDeactivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) != 0)
                return false;

            if (mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return false;

            if (mPlayerMovementComponent.IsGrounded())
                return false;

            return true;
        }

        protected override void OnTick(float deltaTime)
        {
            float inputX = VgInput.GetAxis(InputAxis.LeftStickHorizontal);
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.x = Mathf.Clamp(
                velocity.x + (inputX * mPlayerMovementComponent.AccelerationOnGround * deltaTime),
                -mPlayerMovementComponent.MaxVelocityX,
                mPlayerMovementComponent.MaxVelocityX
            );

            mPlayerMovementComponent.Velocity = velocity;
        }
    }

    public class PlayerInverseAccelerateOnGroundCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.InverseAccelerateOnGround;
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected override bool OnShouldActivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) == 0)
                return false;

            if (mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return false;

            if (!mPlayerMovementComponent.IsGrounded())
                return false;

            return true;
        }

        protected override bool OnShouldDeactivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) != 0)
                return false;

            if (!mPlayerMovementComponent.IsInputAlignedWithVelocityX())
                return false;

            if (mPlayerMovementComponent.IsGrounded())
                return false;

            return true;
        }

        protected override void OnTick(float deltaTime)
        {
            float inputX = Mathf.Abs(VgInput.GetAxis(InputAxis.LeftStickHorizontal));
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                0,
                inputX * mPlayerMovementComponent.InverseAccelerationOnGround * deltaTime
            );

            mPlayerMovementComponent.Velocity = velocity;
        }
    }

    public class PlayerDeAccelerateOnGroundCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.DeAccelerateOnGround;
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected override bool OnShouldActivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) != 0)
                return false;

            if (!mPlayerMovementComponent.IsGrounded())
                return false;

            return true;
        }

        protected override bool OnShouldDeactivate()
        {
            if (VgInput.GetAxis(InputAxis.LeftStickHorizontal) != 0)
                return false;

            if (mPlayerMovementComponent.IsGrounded())
                return false;

            return true;
        }

        protected override void OnTick(float deltaTime)
        {
            var velocity = mPlayerMovementComponent.Velocity;

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                0f,
                mPlayerMovementComponent.DeAccelerationOnGround * deltaTime
            );

            mPlayerMovementComponent.Velocity = velocity;
        }
    }

    public class PlayerAccelerateOnAirCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.AccelerateOnAir;
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

    public class PlayerInverseAccelerateOnAirCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.InverseAccelerateOnAir;
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

    public class PlayerDeAccelerateOnAirCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.DeAccelerateOnAir;
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

    public class PlayerAccelerateWhileOverspeedOnGroundCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.AccelerateWhileOverspeedOnGround;
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

    public class PlayerInverseAccelerateWhileOverspeedOnGroundCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.InverseAccelerateWhileOverspeedOnGround;
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

    public class PlayerDeAccelerateWhileOverspeedOnGroundCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.DeAccelerateWhileOverspeedOnGround;
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

    public class PlayerAccelerateWhileOverspeedOnAirCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.AccelerateWhileOverspeedOnAir;
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

    public class PlayerInverseAccelerateWhileOverspeedOnAirCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.InverseAccelerateWhileOverspeedOnAir;
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

    public class PlayerDeAccelerateWhileOverspeedOnAirCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.Gravity;
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

    public class PlayerGravityCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.Gravity;
            Tags = new List<EccTag> { EccTag.Gravity };
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

    public class PlayerJumpCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.Jump;
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

    public class PlayerJumpApexModifiersCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.JumpApexModifiers;
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
            TickOrderInGroup = PlayerMovementTiceOrder.JumpBuffering;
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
            TickOrderInGroup = PlayerMovementTiceOrder.CoyoteTime;
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

    public class PlayerClampedFallSpeedCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.ClampedFallSpeed;
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

    public class PlayerEdgeSlidingCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.EdgeSliding;
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

    public class PlayerJumpExtraSpeedCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTiceOrder.JumpExtraSpeed;
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
