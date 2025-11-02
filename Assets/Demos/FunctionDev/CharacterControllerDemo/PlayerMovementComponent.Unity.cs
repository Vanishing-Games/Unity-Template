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
            mRigidbody.velocity = Velocity;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log("Collision entered: " + collision.gameObject.name);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            Debug.Log("Collision exited: " + collision.gameObject.name);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            Debug.Log("Collision stayed: " + collision.gameObject.name);
        }

        private void GetAndSetRigidBody()
        {
            mRigidbody ??=
                mGameObject.GetComponent<Rigidbody2D>() ?? mGameObject.AddComponent<Rigidbody2D>();
            // csharpier-ignore-start
            mRigidbody.bodyType                 = RigidbodyType2D.Kinematic;
            mRigidbody.simulated                = true;
            mRigidbody.useFullKinematicContacts = true;
            mRigidbody.collisionDetectionMode   = CollisionDetectionMode2D.Continuous;
            mRigidbody.interpolation            = RigidbodyInterpolation2D.Interpolate;
            mRigidbody.constraints              = RigidbodyConstraints2D.FreezeRotation;
            // csharpier-ignore-end
        }

        private Rigidbody2D mRigidbody;
        private IDisposable mCollisionEnterSubscription;
        private IDisposable mCollisionExitSubscription;
        private IDisposable mCollisionStaySubscription;
    }
}
