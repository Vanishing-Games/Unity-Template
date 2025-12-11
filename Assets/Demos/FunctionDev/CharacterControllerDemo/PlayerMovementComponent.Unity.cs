using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using R3.Triggers;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        protected override void OnSetup()
        {
            GetAndSetRigidBody();
            GetAndSetCapsuleCollider();
            InitializeEvents();

            mCollisionEnterSubscription = mGameObject
                .OnCollisionEnter2DAsObservable()
                .Subscribe(OnCollisionEnter2D);
            mCollisionExitSubscription = mGameObject
                .OnCollisionExit2DAsObservable()
                .Subscribe(OnCollisionExit2D);
            mCollisionStaySubscription = mGameObject
                .OnCollisionStay2DAsObservable()
                .Subscribe(OnCollisionStay2D);
        }

        protected override void OnRemoved()
        {
            mCollisionEnterSubscription.Dispose();
            mCollisionExitSubscription.Dispose();
            mCollisionStaySubscription.Dispose();
        }

        protected override void OnUpdateGo(float deltaTime)
        {
            mRigidbody.linearVelocity = Velocity;
        }

        private void OnCollisionEnter2D(Collision2D collision) { }

        private void OnCollisionExit2D(Collision2D collision) { }

        private void OnCollisionStay2D(Collision2D collision) { }

        private void GetAndSetRigidBody()
        {
            mRigidbody ??=
                mGameObject.GetComponent<Rigidbody2D>() ?? mGameObject.AddComponent<Rigidbody2D>();
            // csharpier-ignore-start
            mGameObject.layer                   = LayerMask.NameToLayer("Character");
            mRigidbody.bodyType                 = RigidbodyType2D.Kinematic;
            mRigidbody.simulated                = true;
            mRigidbody.useFullKinematicContacts = true;
            mRigidbody.collisionDetectionMode   = CollisionDetectionMode2D.Continuous;
            mRigidbody.interpolation            = RigidbodyInterpolation2D.Interpolate;
            mRigidbody.constraints              = RigidbodyConstraints2D.FreezeRotation;
            // csharpier-ignore-end
        }

        private void GetAndSetCapsuleCollider()
        {
            mCollider ??=
                mGameObject.GetComponent<CapsuleCollider2D>()
                ?? mGameObject.AddComponent<CapsuleCollider2D>();
            // csharpier-ignore-start
            mCollider.offset    = Vector2.zero;
            mCollider.size      = new Vector2(1, 2);
            mCollider.direction = CapsuleDirection2D.Vertical;
            // csharpier-ignore-end
        }

        private partial void InitializeEvents()
        {
            // csharpier-ignore-start
            OnPlayerStartJumpEvent     = new();
            OnPlayerEndJumpEvent       = new();
            OnPlayerEnterJumpApexEvent = new();
            OnPlayerLeaveJumpApexEvent = new();
            OnPlayerStartFreefallEvent = new();
            OnPlayerLandedEvent        = new();
            OnPlayerLeaveGroundEvent   = new();
            // csharpier-ignore-end
        }

        private Rigidbody2D mRigidbody;
        private CapsuleCollider2D mCollider;
        private IDisposable mCollisionEnterSubscription;
        private IDisposable mCollisionExitSubscription;
        private IDisposable mCollisionStaySubscription;
    }
}
