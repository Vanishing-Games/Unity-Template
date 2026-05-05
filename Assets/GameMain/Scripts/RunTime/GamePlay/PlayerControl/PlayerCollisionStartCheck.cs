using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UIElements;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerCollisionStartCheck : PlayerControlCapabilityBase
    {
        protected override void OnSetup()
        {
            base.OnSetup();

            HeadHitResults = new RaycastHit2D[1];
            DownHitResults = new RaycastHit2D[1];
            ColliderHitResults = new Collider2D[1];
        }

        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.CollisionStartCheck;
        }

        protected override bool ShouldActivate()
        {
            return true;
        }

        protected override bool ShouldDeactivate()
        {
            return false;
        }

        protected override void OnTick(float deltaTime)
        {
            if (IsCanGrab())
            {
                CornerGrabCheck(mPCComponent.mTranform.position, Vector2.right);
                CornerGrabCheck(mPCComponent.mTranform.position, Vector2.left);
            }

            //左右贴墙判断
            if (mPCComponent.CurrentState == PlayerStateMachine.NormalState)
            {
                ByWallCheck(mPCComponent.mTranform.position, Vector2.right);
                ByWallCheck(mPCComponent.mTranform.position, Vector2.left);
            }

            if (
                (
                    !mPCComponent.IsCornerGrab
                    && mPCComponent.CurrentState == PlayerStateMachine.GrabState
                )
                || mPCComponent.CurrentState == PlayerStateMachine.NormalState
            )
            {
                NormalGrabCheck(mPCComponent.mTranform.position, LayerMask.GetMask("VerticalGrab"));
                NormalSafeGrabCheck(
                    mPCComponent.mTranform.position,
                    LayerMask.GetMask("Hook", "HorizontalGrab")
                );
            }
        }

        private void ByWallCheck(Vector2 PlayerPosition, Vector2 Dir)
        {
            Vector2 FootStartPoint =
                PlayerPosition
                + PlayerColliderOffsetX * Dir
                + new Vector2(0, PlayerFootColliderOffsetY);

            RaycastHit2D hitDown = Physics2D.Raycast(
                FootStartPoint,
                Dir,
                mPCComponent.ByWallCheckDistanceX,
                LayerMask.GetMask("Wall")
            );

            Debug.DrawRay(FootStartPoint, Dir * mPCComponent.ByWallCheckDistanceX, Color.red);

            Vector2 HeadStartPoint =
                PlayerPosition
                + PlayerColliderOffsetX * Dir
                + new Vector2(0, PlayerColliderOffsetUpY);

            RaycastHit2D hitUp = Physics2D.Raycast(
                HeadStartPoint - new Vector2(0, VerticalDistance),
                Dir,
                mPCComponent.ByWallCheckDistanceX,
                LayerMask.GetMask("Wall")
            );

            //左右是否靠墙判断
            if (hitUp.collider != null || hitDown.collider != null)
            {
                if (Dir == Vector2.left)
                    mPCComponent.IsByWallLeft = true;
                else if (Dir == Vector2.right)
                    mPCComponent.IsByWallRight = true;
            }
            else
            {
                if (Dir == Vector2.left)
                    mPCComponent.IsByWallLeft = false;
                else if (Dir == Vector2.right)
                    mPCComponent.IsByWallRight = false;
            }

            if (hitUp.collider != null)
            {
                //滑墙可行性判断
                if (Dir == Vector2.left)
                    mPCComponent.LeftSlideCheck = true;
                else if (Dir == Vector2.right)
                    mPCComponent.RightSlideCheck = true;
            }
            else
            {
                if (Dir == Vector2.left)
                    mPCComponent.LeftSlideCheck = false;
                else if (Dir == Vector2.right)
                    mPCComponent.RightSlideCheck = false;
            }
        }

        private void CornerGrabCheck(Vector2 PlayerPosition, Vector2 Dir)
        {
            Vector2 StartPoint =
                PlayerPosition
                + PlayerColliderOffsetX * Dir
                + new Vector2(0, PlayerColliderOffsetUpY);
            int HeadCount = Physics2D.RaycastNonAlloc(
                StartPoint - new Vector2(0, VerticalDistance),
                Dir,
                HeadHitResults,
                HorizontalDistance,
                LayerMask.GetMask("Wall")
            );

            Debug.DrawRay(
                StartPoint - new Vector2(0, VerticalDistance),
                Dir * HorizontalDistance,
                Color.red
            );

            RaycastHit2D HeadHit;
            if (HeadCount > 0)
            {
                HeadHit = HeadHitResults[0];
                HeadHitResults[0] = default;
            }
            else
            {
                return;
            }

            Vector2 DownStartPoint =
                HeadHit.point + (Vector2.up * VerticalDistance * 2) + Dir * 0.1f;

            int DownCount = Physics2D.RaycastNonAlloc(
                DownStartPoint,
                Vector2.down,
                DownHitResults,
                VerticalDistance * 2f,
                LayerMask.GetMask("Wall")
            );

            RaycastHit2D DownHit = DownHitResults[0];
            DownHitResults[0] = default;

            Debug.DrawRay(DownStartPoint, Vector2.down * VerticalDistance, Color.yellow);
            if (DownHit.collider != null)
            {
                RaycastHit2D[] upHitResults = new RaycastHit2D[2];
                int UpCount = Physics2D.RaycastNonAlloc(
                    DownHit.point - new Vector2(0, 0.2f),
                    Vector2.up,
                    upHitResults,
                    0.3f,
                    LayerMask.GetMask("Wall")
                );

                if (Vector2.Distance(DownHit.point, DownStartPoint) > 0.01 && UpCount == 1)
                {
                    Vector2 targetPoint =
                        DownHit.point
                        - PlayerColliderOffsetX * Dir
                        - new Vector2(0, PlayerColliderOffsetUpY);
                    GrabSet(targetPoint, true, false, Dir);
                }
            }
        }

        private void NormalGrabCheck(Vector2 PlayerPosition, LayerMask noSafeLayer)
        {
            Vector2 BoxRange = new Vector2(mPCComponent.GrabRangeX, mPCComponent.GrabRangeY);
            int count = Physics2D.OverlapBox(
                PlayerPosition
                    + mPCComponent.GrabRangeOffset * new Vector2(mPCComponent.FacingDir, 1),
                BoxRange,
                0f,
                new ContactFilter2D
                {
                    layerMask = noSafeLayer,
                    useLayerMask = true,
                    useTriggers = true,
                },
                ColliderHitResults
            );

            Collider2D hitCollider = ColliderHitResults[0];
            ColliderHitResults[0] = default;

            if (hitCollider != null && IsCanGrab())
            {
                Bounds GrabBounds = hitCollider.bounds;
                float centerX = (float)GrabBounds.center.x;
                Vector2 targetPoint =
                    new Vector2(centerX, PlayerPosition.y)
                    - new Vector2(
                        mPCComponent.FacingDir * mPCComponent.GrabRangeOffset.x,
                        mPCComponent.GrabRangeOffset.y
                    );
                GrabSet(targetPoint, false, false, new Vector2(-1 * mPCComponent.FacingDir, 0));
            }
            else if (
                hitCollider == null
                && mPCComponent.CurrentState == PlayerStateMachine.GrabState
                && mPCComponent.IsSafeGrab == false
            )
            {
                SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
            }
        }

        private void NormalSafeGrabCheck(Vector2 PlayerPosition, LayerMask SafeLayer)
        {
            Vector2 BoxRange = new Vector2(mPCComponent.GrabRangeX, mPCComponent.GrabRangeY);
            int count = Physics2D.OverlapBox(
                PlayerPosition
                    + mPCComponent.GrabRangeOffset * new Vector2(mPCComponent.FacingDir, 1),
                BoxRange,
                0f,
                new ContactFilter2D
                {
                    layerMask = SafeLayer,
                    useLayerMask = true,
                    useTriggers = true,
                },
                ColliderHitResults
            );

            Vector2 center =
                PlayerPosition
                + mPCComponent.GrabRangeOffset * new Vector2(mPCComponent.FacingDir, 1)
                + new Vector2(0, 0.4f);

            // 2. 计算矩形的四个顶点
            float halfX = BoxRange.x / 2f;
            float halfY = BoxRange.y / 2f;

            Vector2 topLeft = center + new Vector2(-halfX, halfY);
            Vector2 topRight = center + new Vector2(halfX, halfY);
            Vector2 bottomLeft = center + new Vector2(-halfX, -halfY);
            Vector2 bottomRight = center + new Vector2(halfX, -halfY);

            // 3. 使用 Debug.DrawLine 连接顶点
            Color drawColor = Color.green; // 你可以根据 count > 0 切换颜色
            Debug.DrawLine(topLeft, topRight, drawColor);
            Debug.DrawLine(topRight, bottomRight, drawColor);
            Debug.DrawLine(bottomRight, bottomLeft, drawColor);
            Debug.DrawLine(bottomLeft, topLeft, drawColor);

            Collider2D hitCollider = ColliderHitResults[0];
            GameObject hitGO = null;
            if (hitCollider != null)
                hitGO = ColliderHitResults[0].gameObject;

            ColliderHitResults[0] = default;

            if (hitCollider != null && IsCanGrab())
            {
                Bounds GrabBounds = hitCollider.bounds;
                float centerX = (float)GrabBounds.center.x;
                float centerY = (float)GrabBounds.center.y;
                Vector2 targetPoint =
                    new Vector2(centerX, PlayerPosition.y)
                    - new Vector2(
                        mPCComponent.FacingDir * mPCComponent.GrabRangeOffset.x,
                        mPCComponent.GrabRangeOffset.y
                    );
                if (LayerMask.LayerToName(hitGO.layer) == "HorizontalGrab")
                    targetPoint =
                        new Vector2(GrabBounds.ClosestPoint(PlayerPosition).x, centerY)
                        - new Vector2(
                            mPCComponent.FacingDir * mPCComponent.GrabRangeOffset.x,
                            mPCComponent.GrabRangeOffset.y + 0.4f
                        );
                GrabSet(targetPoint, false, true, new Vector2(-1 * mPCComponent.FacingDir, 0));
            }
            else if (hitCollider == null && mPCComponent.IsSafeGrab)
            {
                count = Physics2D.OverlapBox(
                    PlayerPosition
                        + mPCComponent.GrabRangeOffset * new Vector2(mPCComponent.FacingDir, 1)
                        + new Vector2(0, 0.4f),
                    BoxRange,
                    0f,
                    new ContactFilter2D
                    {
                        layerMask = SafeLayer,
                        useLayerMask = true,
                        useTriggers = true,
                    },
                    ColliderHitResults
                );
                hitCollider = ColliderHitResults[0];
                ColliderHitResults[0] = default;
                if (hitCollider == null)
                    SetStateMachine(PlayerStateMachine.NormalState, EccTag.NormalState);
            }
        }

        private void GrabSet(Vector2 targetPoint, bool IsCorner, bool IsSafe, Vector2 dir)
        {
            SetStateMachine(PlayerStateMachine.GrabState, EccTag.GrabState);
            mPCComponent.CurrentState = PlayerStateMachine.GrabState;
            mPCComponent.IsCornerGrab = IsCorner;
            mPCComponent.IsSafeGrab = IsSafe;
            mPCComponent.CtrlVelocity = Vector2.zero;

            mPCComponent.FacingDir = (int)dir.x * -1;
            mPCComponent.mTranform.position = targetPoint;
        }

        private RaycastHit2D[] HeadHitResults = new RaycastHit2D[1];
        private RaycastHit2D[] DownHitResults = new RaycastHit2D[1];
        private Collider2D[] ColliderHitResults = new Collider2D[1];

        private bool IsCanGrab()
        {
            if (mPCComponent.IsBroadGrab)
            {
                return !IsOnGround
                    && mPCComponent.CurrentState == PlayerStateMachine.NormalState
                    && !mPCComponent.IsJumping
                    && mPCComponent.CanGrabCDTimer == 0;
            }
            else
            {
                return !IsOnGround
                    && mPCComponent.CurrentState == PlayerStateMachine.NormalState
                    && mPCComponent.CtrlVelocity.y < mPCComponent.GrabThresholdSpeedY;
            }
        }

        private float PlayerColliderOffsetX => mPCComponent.mBoxCollider.size.x * 0.5f;

        private float PlayerHeadColliderOffsetY =>
            mPCComponent.mBoxCollider.size.y * 0.5f + mPCComponent.mBoxCollider.offset.y;

        private float PlayerFootColliderOffsetY =>
            mPCComponent.mBoxCollider.size.y * 0.5f * -1f + mPCComponent.mBoxCollider.offset.y;

        private float PlayerColliderOffsetUpY =>
            PlayerHeadColliderOffsetY + mPCComponent.CornerGrabStartOffsetY;

        private float HorizontalDistance => mPCComponent.CornerGrabOffsetX;
        private float VerticalDistance => mPCComponent.CornerGrabOffsetY;
    }
}
