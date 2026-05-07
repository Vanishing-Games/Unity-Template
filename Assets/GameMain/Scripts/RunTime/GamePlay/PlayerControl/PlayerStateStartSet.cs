using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public class PlayerStateStartSet : PlayerControlCapabilityBase
    {
        protected override void SetUpTickSettings()
        {
            base.SetUpTickSettings();
            TickOrderInGroup = (uint)PlayerControlTickOrder.StateStartSet;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            BeeAddSubscription = MessageBroker.Global.Subscribe<BeeManagerEvents.BeeAddEvents>(
                AddBeeToList
            );
            BeeRemoveSubscription =
                MessageBroker.Global.Subscribe<BeeManagerEvents.BeeRemoveEvents>(RemoveBeeFromList);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            BeeAddSubscription.Dispose();
            BeeRemoveSubscription.Dispose();
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
            //MoveX管理
            if (mPCComponent.ForceMoveXRevTimer > 0)
            {
                mPCComponent.MoveX = mPCComponent.ForceMoveX;
                mPCComponent.ForceMoveXRevTimer--;
            }
            else
            {
                mPCComponent.MoveX = (int)Math.Sign(mPCComponent.InputX);
            }

            //角色朝向修改
            if (MoveX != 0 && mPCComponent.CurrentState == PlayerStateMachine.NormalState)
                mPCComponent.FacingDir = mPCComponent.MoveX * -1;

            //角色是否能投掷 --
            if (
                PreThrowInputTimer > 0
                && PreThrowInputTimer < mPCComponent.PreThrowTime
                && mPCComponent.CurrentState == PlayerStateMachine.NormalState
            )
                mPCComponent.IsCanThrow = CanThrowCheck();
            else
                mPCComponent.IsCanThrow = false;

            //角色口哨cd计时器
            if (mPCComponent.WhistleCDTimer < mPCComponent.WhistleCDTime)
                mPCComponent.WhistleCDTimer++;

            //角色口哨预输入时间
            if (mPCComponent.InputAct2 && mPCComponent.PreWhistleInputTimer > 0)
                mPCComponent.PreWhistleInputTimer--;
            else if (!mPCComponent.InputAct2)
                mPCComponent.PreWhistleInputTimer = mPCComponent.PreWhistleInputTime;

            //角色跳跃输入计时器
            if (InputJump && mPCComponent.PreJumpInputTimer > 0)
                mPCComponent.PreJumpInputTimer--;
            else if (!InputJump)
                mPCComponent.PreJumpInputTimer = PreJumpInputTime;
            //角色投掷输入计时器
            if (InputAct && mPCComponent.PreThrowInputTimer > 0)
                mPCComponent.PreThrowInputTimer--;
            else if (!InputAct)
                mPCComponent.PreThrowInputTimer = mPCComponent.PreThrowTime;
            //狼跳计时器
            if (IsOnGround)
                mPCComponent.CoyoteJumpInputRevTimer = CoyoteJumpInputTime;
            else if (mPCComponent.CoyoteJumpInputRevTimer > 0)
                mPCComponent.CoyoteJumpInputRevTimer--;
            //墙跳土狼计时器
            if (mPCComponent.IsByWallLeft || mPCComponent.IsByWallRight)
            {
                mPCComponent.WallCoyoteJumpInputRevTimer = mPCComponent.WallCoyoteJumpInputTime;
                if (mPCComponent.IsByWallLeft)
                    mPCComponent.WallDir = 1;
                else
                    mPCComponent.WallDir = -1;
            }
            else if (mPCComponent.WallCoyoteJumpInputRevTimer > 0)
                mPCComponent.WallCoyoteJumpInputRevTimer--;
            //滑落计时器
            if (mPCComponent.CurrentState != PlayerStateMachine.GrabState)
                mPCComponent.GrabStayRevTimer = mPCComponent.GrabStayTime;
            //投掷cd计时器 --
            if (
                (
                    mPCComponent.CurrentState != PlayerStateMachine.ThrowState
                    || mPCComponent.CurrentState != PlayerStateMachine.ThrowState
                )
                && mPCComponent.ThrowCdInputTimer > 0
            )
                mPCComponent.ThrowCdInputTimer--;
            //抓跳后再抓cd计时器
            if (mPCComponent.CanGrabCDTimer > 0 && !mPCComponent.IsJumping)
                mPCComponent.CanGrabCDTimer--;

            //角色跟随特效开关
            if (mPCComponent.IsOnGround && Mathf.Abs(mPCComponent.CtrlVelocity.x) > 4f)
            {
                if (mPCComponent.FollowEffect != null)
                    mPCComponent.FollowEffect.SetActive(true);
            }
            else
            {
                if (mPCComponent.FollowEffect != null)
                    mPCComponent.FollowEffect.SetActive(false);
            }
        }

        void AddBeeToList(BeeManagerEvents.BeeAddEvents e)
        {
            Debug.Log(e.Bee);
            if (!mPCComponent.AllBees.Contains(e.Bee))
            {
                mPCComponent.AllBees.Add(e.Bee);
                Debug.Log("添加成功");
            }
        }

        void RemoveBeeFromList(BeeManagerEvents.BeeRemoveEvents e)
        {
            if (mPCComponent.AllBees.Contains(e.Bee))
            {
                mPCComponent.AllBees.Remove(e.Bee);
            }
        }

        bool CanThrowCheck()
        {
            //BeeMainControl BeeToThrow = mPCComponent.AllBees.FirstOrDefault
            //	(f => {
            //		bool isMatch = (f.currentState == BeeState.FollowSt);
            //		Debug.Log($"检查飞虫 {f.name}，是否匹配: {isMatch}");
            //		return isMatch;
            //	});
            BeeMainControl BeeToThrow = null;

            if (mPCComponent.AllBees.Count > 0)
            {
                for (int i = 0; i < mPCComponent.AllBees.Count; i++)
                {
                    if (mPCComponent.AllBees[i] != null)
                    {
                        if (
                            mPCComponent.AllBees[i].GetComponent<BeeMainControl>().currentState
                            == BeeState.FollowSt
                        )
                        {
                            BeeToThrow = mPCComponent.AllBees[i].GetComponent<BeeMainControl>();
                            break;
                        }
                    }
                }
            }

            if (BeeToThrow != null)
            {
                mPCComponent.BeeToThrow = BeeToThrow;
                return true;
            }
            else
            {
                mPCComponent.BeeToThrow = null;
                return false;
            }
        }

        private IDisposable BeeAddSubscription;
        private IDisposable BeeRemoveSubscription;
    }
}
