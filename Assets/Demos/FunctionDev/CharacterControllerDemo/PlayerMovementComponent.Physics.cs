using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        public Vector2 CapsuleCollierOffset() => mCollider.offset;
        public Vector2 CapsuleCollierSize() => mCollider.size;
        public CapsuleDirection2D CpasuleCollierDirection() => mCollider.direction;
    }
}
