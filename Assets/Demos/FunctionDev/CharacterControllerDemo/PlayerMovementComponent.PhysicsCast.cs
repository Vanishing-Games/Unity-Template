using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        public bool GrabCheck_Platform()
        {
            return PlatformGrabCheck_TopBoxCheck() && PlatformGrabCheck_ButtomBoxCheck();
        }

        private bool PlatformGrabCheck_TopBoxCheck()
        {
            var pos =
                (Vector2)mTransform.position
                + new Vector2(
                    PlatformCheckBoxOffsetX * Mathf.Sign(Velocity.x),
                    PlatformTopCheckBoxOffsetY
                );

            var hit = Physics2D.OverlapBox(
                pos,
                new Vector2(PlatformCheckBoxWidth, PlatformTopCheckBoxHeight),
                0,
                LayerMask.GetMask("Static Object")
            );

            return hit == null;
        }

        private bool PlatformGrabCheck_ButtomBoxCheck()
        {
            var pos =
                (Vector2)mTransform.position
                + new Vector2(
                    PlatformCheckBoxOffsetX * Mathf.Sign(Velocity.x),
                    PlatformButtomCheckBoxOffsetY
                );

            var hit = Physics2D.OverlapBox(
                pos,
                new Vector2(PlatformCheckBoxWidth, PlatformButtomCheckBoxHeight),
                0,
                LayerMask.GetMask("Static Object")
            );

            return hit != null;
        }
    }
}
