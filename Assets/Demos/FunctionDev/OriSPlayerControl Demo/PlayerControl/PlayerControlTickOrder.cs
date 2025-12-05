
namespace PlayerControlByOris
{
    public enum PlayerControlTickOrder
	{
		CollisionStartCheck,
		StateStartSet,
		HorizontalControl,
		GravityControl,
		GrabJumpControl,
		JumpControl,
		CollisionEndCheck,
		StateEndSet,
	}
}
