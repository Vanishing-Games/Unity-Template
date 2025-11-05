using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        public Vector2 CapsuleColliderOffset() => mCollider.offset;

        public Vector2 CapsuleColliderSize() => mCollider.size;

        public CapsuleDirection2D CpasuleCollierDirection() => mCollider.direction;

        public CapsuleCollider2D GetCapsuleCollider() => mCollider;
    }
}
