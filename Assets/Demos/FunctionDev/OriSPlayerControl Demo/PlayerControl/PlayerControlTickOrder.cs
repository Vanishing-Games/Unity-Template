
namespace PlayerControlByOris
{
    public enum PlayerControlTickOrder
	{
		InitialSet,
		CollisionStartCheck,
		StateStartSet,
		ThrowControl,
		DashControl,
		HorizontalControl,
		GrabGravity,
		GravityControl,
		GrabJumpControl,
		JumpControl,
		CollisionEndCheck,
		StateEndSet,
	}
}
