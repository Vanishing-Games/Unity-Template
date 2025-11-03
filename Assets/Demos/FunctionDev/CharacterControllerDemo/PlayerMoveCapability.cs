using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
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
}
