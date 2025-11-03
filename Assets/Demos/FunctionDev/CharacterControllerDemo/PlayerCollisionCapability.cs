using System.Collections.Generic;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public class PlayerEdgeSlidingCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.EdgeSliding;
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
