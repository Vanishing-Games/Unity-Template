using System.Collections.Generic;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public class PlayerGravityCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTickOrder.Gravity;
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

    public class PlayerClampedFallSpeedCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = PlayerMovementTickOrder.ClampedFallSpeed;
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
