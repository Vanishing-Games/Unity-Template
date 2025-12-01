using VanishingGames.ECC.Runtime;

namespace PlayerControlByOris
{
    public abstract class PlayerControlCapabilityBase : EccCapability
    {
		protected override void OnActivate() { }

		protected override void OnDeactivate() { }

		protected override void OnSetup()
		{
			mPCComponent = mOwner.GetEccComponent<PlayerControlComponent>();
		}

		protected override void SetUpTickSettings()
		{
			TickGroup = EccTickGroup.Movement;
			TickType = EccTickType.Fixed;
		}

		protected PlayerControlComponent mPCComponent;
    }
}
