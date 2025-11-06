using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    // csharpier-ignore-start
    public enum PlayerMoveState
    {
        OnGround , // 在地面上时
        OnAir    , // 在空中时
        Jumping  , // 跳跃时 (包括: 起跳,上升)
        Apex     , // 在跳跃顶点时 (即在 ApexModifier 时间窗口内)
    }
    // csharpier-ignore-end

    public partial class PlayerMovementComponent : EccComponent
    {
        public Vector2 Position() => mTransform.position;

        public void SetPosition(Vector2 position) => mTransform.position = position;

        public Vector2 VelocityNormalized() => Velocity.normalized;

        /// <summary>
        /// whether the player touches the ground, using a fast method, which just returns the IsOnGround property
        /// </summary>
        public bool IsOnGroundFast()
        {
            return IsOnGround;
        }

        /// <summary>
        /// whether the player touches the ground, using box cast, will update the IsOnGround property
        /// </summary>
        public bool UpdateOnGroundStatus()
        {
            var hit = Physics2D.BoxCast(
                mTransform.position,
                new Vector2(1.0f, 1.0f),
                0,
                Vector2.down,
                GroundCheckDistance + CapsuleColliderSize().y * 0.5f,
                LayerMask.GetMask("Static Object")
            );

            IsOnGround = hit.collider != null;
            return IsOnGround;
        }

        /// <summary>
        /// whether the player touches the ceiling, using a fast method, which just returns the IsUnderCeiling property
        /// </summary>
        public bool IsUnderCeilingFast()
        {
            return IsUnderCeiling;
        }

        /// <summary>
        /// whether the player touches the ceiling, using box cast, will update the IsUnderCeiling property
        /// </summary>
        public bool UpdateUnderCeilingStatus()
        {
            var hit = Physics2D.BoxCast(
                mTransform.position,
                new Vector2(1.0f, 1.0f),
                0,
                Vector2.up,
                GroundCheckDistance + CapsuleColliderSize().y * 0.5f,
                LayerMask.GetMask("Static Object")
            );

            IsUnderCeiling = hit.collider != null;
            return IsUnderCeiling;
        }

        /// <summary>
        /// whether the player reaches the max jump time
        /// </summary>
        public bool ReachesMaxJumpTime() => JumpScaledTimeSinceStarted >= MaxJumpScaledTime;

        /// <summary>
        /// whether the player is within the jump apex modifier tise window
        /// </summary>
        public bool WithinJumpApexModifierTime() =>
            ReachesMaxJumpTime()
            && JumpApexModifierScaledTimeSinceStarted < JumpApexModifiersScaledTime;

        public bool WithinCoyoteTime() => !IsOnGround && ScaledTimeInAir < CoyoteJumpScaledTime;

        public bool IsOverSpeed() => Velocity.x > MaxVelocityX;

        public bool IsInputAlignedWithVelocityX() =>
            Velocity.x == 0 || VgInput.GetAxis(InputAxis.LeftStickHorizontal) * Velocity.x > 0;

        public bool IsMovingRight() => Velocity.x > 0;

        public bool IsMovingLeft() => Velocity.x < 0;

        public bool IsJumpRising() => CurrentMovementState == PlayerMoveState.Jumping;

        public bool IsJumping() =>
            CurrentMovementState == PlayerMoveState.Jumping
            || CurrentMovementState == PlayerMoveState.Apex;

        public bool IsFalling() => CurrentMovementState == PlayerMoveState.OnAir;

        public bool IsAtJumpApex() => CurrentMovementState == PlayerMoveState.Apex;
    }
}
