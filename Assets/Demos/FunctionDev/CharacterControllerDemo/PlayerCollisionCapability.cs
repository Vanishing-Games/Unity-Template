using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public class PlayerEdgeSlidingCapability : PlayerMoveCapability
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.EdgeSliding;
            Tags = new List<EccTag> { EccTag.Move };
        }

        protected override bool OnShouldActivate()
        {
            return CheckCollide();
        }

        protected override bool OnShouldDeactivate()
        {
            return !CheckCollide();
        }

        protected override void OnTick(float deltaTime) { }

        private bool CheckCollide()
        {
            var origin = mPlayerMovementComponent.Position();
            var direction = mPlayerMovementComponent.VelocityNormalized();
            var capsuleDirection = mPlayerMovementComponent.CpasuleCollierDirection();

            var hitCount = Physics2D.CapsuleCastNonAlloc(
                origin,
                direction,
                capsuleDirection,
                0,
                direction,
                mHits
            );

            return hitCount != 0;
        }

        // csharpier-ignore-start
        public const float COLLIDE_SKIN_WIDTH = 0.015f;
        public  const int RECURSIVE_DEPTH      = 5;
        private const int HIT_ARRAY_SIZE       = 32;
        private readonly RaycastHit2D[] mHits  = new RaycastHit2D[HIT_ARRAY_SIZE];
        // csharpier-ignore-end
    }
}
