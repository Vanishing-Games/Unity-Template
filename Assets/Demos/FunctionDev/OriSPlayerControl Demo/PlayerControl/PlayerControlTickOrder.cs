
namespace PlayerControlByOris
{
    public enum PlayerControlTickOrder
	{
		InitialSet,
		CollisionStartCheck,
		StateStartSet,
		HorizontalControl,
		GrabGravity,
		GravityControl,
		GrabJumpControl,
		JumpControl,
		CollisionEndCheck,
		StateEndSet,
	}
}
