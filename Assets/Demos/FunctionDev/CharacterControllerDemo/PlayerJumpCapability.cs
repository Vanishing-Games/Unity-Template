using System.Collections.Generic;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public class PlayerJumpCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTickOrder.Jump;
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
            TickOrderInGroup = PlayerMovementTickOrder.JumpApexModifiers;
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
            TickOrderInGroup = PlayerMovementTickOrder.JumpBuffering;
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
            TickOrderInGroup = PlayerMovementTickOrder.CoyoteTime;
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
            TickOrderInGroup = PlayerMovementTickOrder.JumpExtraSpeed;
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
