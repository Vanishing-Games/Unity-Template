using System.Collections.Generic;
using UnityEngine;
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

		protected float Approach (float currentValue, float targetValue, float Accelerate)
		{
			float backValue = targetValue;
			if (currentValue > targetValue)
			{
				backValue = currentValue - Accelerate;
				if (backValue < targetValue)
					backValue = targetValue;
			}
			else if (currentValue < targetValue)
			{
				backValue = currentValue + Accelerate;
				if (backValue > targetValue)
					backValue = targetValue;
			}
			return backValue;
		}



		protected void SetStateMachine(PlayerStateMachine ToState,EccTag ToTag)
		{
			mPCComponent.CurrentState = ToState;
			List<EccTag> CurrentTag = new List<EccTag> { ToTag };
			mOwner.UnblockCapabilities(StateTag);
			List<EccTag> totalTags = new() { EccTag.NormalState, EccTag.GrabState, EccTag.DashState, EccTag.DeathState };
			foreach (var tag in totalTags)
			{
				if (tag != ToTag)
					mOwner.BlockCapabilities(tag, StateTag);
			}
		}

		protected static IEccInstigator StateTag;

		protected PlayerControlComponent mPCComponent;
		protected BoxCollider2D mBoxCollision;
		protected bool IsOnGround => mPCComponent.IsOnGround;
		protected bool IsJumping => mPCComponent.IsJumping;

		protected bool InputJump => mPCComponent.InputJump;

		protected float JumpSpeedY => mPCComponent.JumpSpeedY;
		protected float JumpBoostSpeedX => mPCComponent.JumpBoostSpeedX;
		protected int MinJumpTime => mPCComponent.MinJumpTime;
		protected int MaxJumpTime => mPCComponent.MaxJumpTime;
		protected int PreJumpInputTime => mPCComponent.PreJumpInputTime;
		protected int CoyoteJumpInputTime => mPCComponent.CoyoteJumpInputTime;

		protected float MaxSpeedX => mPCComponent.MaxSpeedX;
		protected float AccX => mPCComponent.AccX;
		protected float OverReduceX => mPCComponent.OverReduceX;
		protected float AirAccMultX => mPCComponent.AirAccMultX;
		protected float MoveX => mPCComponent.MoveX;

		protected float MaxFallSpeedY => mPCComponent.MaxFallSpeedY;
		protected float GravityAccY => mPCComponent.GravityAccY;
		protected float LowGravThresholdSpeedY => mPCComponent.LowGravThresholdSpeedY;
		protected float LowGravMult => mPCComponent.LowGravMult;
	}
}
