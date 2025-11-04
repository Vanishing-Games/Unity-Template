using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        public Vector2 Position() => mTransform.position;

        public Vector2 VelocityNormalized() => Velocity.normalized;

        public bool IsGrounded()
        {
            var hit = Physics2D.Raycast(
                mTransform.position,
                Vector2.down,
                GroundCheckDistance + CapsuleColliderSize().y * 0.5f,
                LayerMask.GetMask("Static Object")
            );

            return hit.collider != null;
        }

        public bool IsOverSpeed() => Velocity.x > MaxVelocityX;

        public bool IsInputAlignedWithVelocityX() =>
            Velocity.x == 0 || VgInput.GetAxis(InputAxis.LeftStickHorizontal) * Velocity.x > 0;

        public bool IsMovingRight() => Velocity.x > 0;

        public bool IsMovingLeft() => Velocity.x < 0;
    }
}
