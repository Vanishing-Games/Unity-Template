using System;
using System.Collections.Generic;
using Core;
using R3;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public class PlayerEdgeSlidingCapability : PlayerMoveCapability
    {
        protected override void OnSetup()
        {
            base.OnSetup();

#if UNITY_EDITOR
            mCollideAndSlideDebugger = CollideAndSlideDebugger.Instance;
            mUpdateSubscription = Observable.EveryUpdate().Subscribe(_ => UpdateDebugger());
#endif
        }

        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerMovementTickOrder.EdgeSliding;
            Tags = new List<EccTag> { EccTag.CollideAndSlide };
        }

        protected override bool OnShouldActivate()
        {
            return CheckCollide();
        }

        protected override bool OnShouldDeactivate()
        {
            return !CheckCollide();
        }

        protected override void OnTick(float deltaTime)
        {
            var velocity = mPlayerMovementComponent.Velocity;
            var position = mPlayerMovementComponent.Position();

            var newVelocity = CollideAndSlide(velocity, position);
            mPlayerMovementComponent.Velocity = newVelocity;

#if UNITY_EDITOR
            mOriginalVelocity = velocity;
            mNewVelocity = newVelocity;
#endif
        }

        private Vector2 CollideAndSlide(Vector2 velocity, Vector2 position, uint depth = 0)
        {
            if (depth > RECURSIVE_DEPTH)
                return velocity;

            if (CheckCollide())
            {
                Vector2 snapToSurface = velocity.normalized * (mHit.distance - mColliderSkinWidth);
                Vector2 leftVelocity = velocity - snapToSurface;

                if (snapToSurface.magnitude <= mColliderSkinWidth)
                    snapToSurface = Vector2.zero;

                float leftMagnitude = leftVelocity.magnitude;
                leftVelocity = leftVelocity.ProjectOnLine(mHit.normal).normalized;
                leftVelocity *= leftMagnitude;

                return snapToSurface
                    + CollideAndSlide(leftVelocity, position + snapToSurface, ++depth);
            }

            return velocity;
        }

        private bool CheckCollide()
        {
            if (mPlayerMovementComponent.Velocity.magnitude <= 0.0f)
                return false;

            var origin =
                mPlayerMovementComponent.Position()
                + mPlayerMovementComponent.VelocityNormalized() * mColliderSkinWidth;
            var size = mPlayerMovementComponent.CapsuleColliderSize();
            size -= new Vector2(mColliderSkinWidth, mColliderSkinWidth);
            var direction = mPlayerMovementComponent.VelocityNormalized();
            var capsuleDirection = mPlayerMovementComponent.CpasuleCollierDirection();
            var distance =
                mPlayerMovementComponent.Velocity.magnitude * Time.deltaTime + mColliderSkinWidth;
            var layerMask = LayerMask.GetMask("Static Object");

            mHit = Physics2D.CapsuleCast(
                origin,
                size,
                capsuleDirection,
                0,
                direction,
                distance,
                layerMask
            );
            return mHit.collider != null;
        }

        // csharpier-ignore-start
        public  const int RECURSIVE_DEPTH = 5;
        public  float mColliderSkinWidth  = 0.05f;
        private RaycastHit2D mHit         = new ();
        // csharpier-ignore-end

#if UNITY_EDITOR
        private void UpdateDebugger()
        {
            if (mCollideAndSlideDebugger == null)
                return;

            var origin = mPlayerMovementComponent.Position();
            var size = mPlayerMovementComponent.CapsuleColliderSize();
            size -= new Vector2(mColliderSkinWidth, mColliderSkinWidth);
            var direction = mPlayerMovementComponent.VelocityNormalized();
            var capsuleDirection = mPlayerMovementComponent.CpasuleCollierDirection();
            var distance =
                mPlayerMovementComponent.Velocity.magnitude * Time.deltaTime + mColliderSkinWidth;

            mCollideAndSlideDebugger.UpdateCapsuleCastInfo(
                mHit,
                origin,
                direction,
                distance,
                size,
                capsuleDirection,
                mOriginalVelocity,
                mNewVelocity
            );
        }

        private IDisposable mUpdateSubscription;
        private CollideAndSlideDebugger mCollideAndSlideDebugger;
        private Vector2 mOriginalVelocity;
        private Vector2 mNewVelocity;
#endif
    }
}
