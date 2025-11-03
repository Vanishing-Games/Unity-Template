using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        public bool IsGrounded()
        {
            return true;
        }

        public bool IsInputAlignedWithVelocityX() =>
            Velocity.x == 0 || VgInput.GetAxis(InputAxis.LeftStickHorizontal) * Velocity.x > 0;

        public bool IsMovingRight() => Velocity.x > 0;

        public bool IsMovingLeft() => Velocity.x < 0;
    }
}
