
namespace PlayerControlByOris
{
    public enum PlayerControlTickOrder
	{
		CollisionStartCheck,
		StateStartSet,
		HorizontalControl,
		GravityControl,
		JumpControl,
		CollisionEndCheck,
		StateEndSet,
	}
}
