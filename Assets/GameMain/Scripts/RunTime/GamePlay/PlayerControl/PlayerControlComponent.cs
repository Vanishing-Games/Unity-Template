using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace GameMain.RunTime
{
    public enum PlayerStateMachine
    {
        NormalState,
        GrabState,
        WhistleState,
        ThrowState,
        DashState,
        DeathState,
    }

    public partial class PlayerControlComponent : EccComponent
    {
        [HideInInspector]
        public GameObject ThrownHook;

        [BoxGroup("预制体"), Tooltip("投出的勾绳"), ShowInInspector, OdinSerialize]
        public GameObject PreHook;

        [BoxGroup("预制体"), Tooltip("口哨的波纹"), ShowInInspector, OdinSerialize]
        public GameObject PreWave;

        [BoxGroup("飞虫管理"), Tooltip("存储的飞虫"), ShowInInspector, OdinSerialize]
        public List<GameObject> AllBees = new List<GameObject>();

        [BoxGroup("飞虫管理"), Tooltip("将要被投掷的飞虫"), ShowInInspector, OdinSerialize]
        public BeeMainControl BeeToThrow { get; set; }

        #region 状态变量信息
        [BoxGroup("状态变量信息"), Tooltip("角色状态机"), ShowInInspector, ReadOnly]
        public PlayerStateMachine CurrentState { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否在地面状态判断"), ShowInInspector, ReadOnly]
        public bool IsOnGround { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否跳跃状态判断"), ShowInInspector, ReadOnly]
        public bool IsJumping { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否左侧靠墙判断"), ShowInInspector, ReadOnly]
        public bool IsByWallLeft { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否左侧靠墙判断"), ShowInInspector, ReadOnly]
        public bool IsByWallRight { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否靠墙下滑判断"), ShowInInspector, ReadOnly]
        public bool IsWallSlide { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否抓墙角判断"), ShowInInspector, ReadOnly]
        public bool IsCornerGrab { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否抓稳定点判断"), ShowInInspector, ReadOnly]
        public bool IsSafeGrab { get; set; }

        [BoxGroup("状态变量信息"), Tooltip("是否可以投掷"), ShowInInspector, ReadOnly]
        public bool IsCanThrow { get; set; }

        #endregion

        #region 按键输入信息

        [BoxGroup("按键输入信息"), Tooltip("X轴方向输入"), ShowInInspector, ReadOnly]
        public float InputX { get; set; }

        [BoxGroup("按键输入信息"), Tooltip("Y轴方向输入"), ShowInInspector, ReadOnly]
        public float InputY { get; set; }

        [BoxGroup("按键输入信息"), Tooltip("跳跃按键输入"), ShowInInspector, ReadOnly]
        public bool InputJump { get; set; }

        [BoxGroup("按键输入信息"), Tooltip("特殊能力按键输入"), ShowInInspector, ReadOnly]
        public bool InputAct { get; set; }

        [BoxGroup("按键输入信息"), Tooltip("特殊能力按键2输入"), ShowInInspector, ReadOnly]
        public bool InputAct2 { get; set; }
        #endregion

        #region 角色基本属性

        [BoxGroup("角色基本属性"), Tooltip("角色面朝方向"), ShowInInspector, ReadOnly]
        public int FacingDir { get; set; } = 1;

        [BoxGroup("角色基本属性"), Tooltip("角色将要移动方向"), ShowInInspector, ReadOnly]
        public int MoveX { get; set; } = 0;

        [BoxGroup("角色基本属性"), Tooltip("角色被强制的移动方向"), ShowInInspector, ReadOnly]
        public int ForceMoveX { get; set; } = 0;

        [BoxGroup("角色基本属性"), Tooltip("角色最终总速度"), ShowInInspector, ReadOnly]
        public Vector2 TotalVelocity { get; set; } = Vector2.zero;

        [BoxGroup("角色基本属性"), Tooltip("角色通过外界获得的额外速度"), ShowInInspector, ReadOnly]
        public Vector2 ExtraVelocity { get; set; } = Vector2.zero;

        [
            BoxGroup("角色基本属性"),
            Tooltip("角色在脚本内可以被控制的速度"),
            ShowInInspector,
            ReadOnly
        ]
        public Vector2 CtrlVelocity { get; set; } = Vector2.zero;

        #endregion

        #region 角色输入计时器

        [
            BoxGroup("角色输入计时器"),
            Tooltip("预输入跳跃按键时长计时器（单位：帧）"),
            ShowInInspector,
            ReadOnly
        ]
        public int PreJumpInputTimer { get; set; } = 0;

        [
            BoxGroup("角色输入计时器"),
            Tooltip("土狼跳输入窗口计时器（单位：帧）"),
            ShowInInspector,
            ReadOnly
        ]
        public int CoyoteJumpInputRevTimer { get; set; } = 0;

        [
            BoxGroup("角色输入计时器"),
            Tooltip("土狼跳输入窗口计时器（单位：帧）"),
            ShowInInspector,
            ReadOnly
        ]
        public int WallCoyoteJumpInputRevTimer { get; set; } = 0;

        [BoxGroup("角色输入计时器"), Tooltip("投掷预输入计时器"), ShowInInspector, ReadOnly]
        public int PreThrowInputTimer { get; set; } = 0;

        [BoxGroup("角色输入计时器"), Tooltip("投掷输入cd计时器"), ShowInInspector, ReadOnly]
        public int ThrowCdInputTimer { get; set; } = 0;

        [BoxGroup("角色输入计时器"), Tooltip("口哨预输入按键计时器"), ShowInInspector, ReadOnly]
        public int PreWhistleInputTimer { get; set; }

        #endregion

        #region 角色运行计时器
        [
            BoxGroup("角色运行计时器"),
            Tooltip("跳跃保持计时器（单位：帧）"),
            ShowInInspector,
            ReadOnly
        ]
        public int JumpingTimer { get; set; } = 0;

        [
            BoxGroup("角色运行计时器"),
            Tooltip("失控水平移动计时器（单位：帧）"),
            ShowInInspector,
            ReadOnly
        ]
        public int ForceMoveXRevTimer { get; set; } = 0;

        [
            BoxGroup("角色运行计时器"),
            Tooltip("跳跃保持计时器（单位：帧）"),
            ShowInInspector,
            ReadOnly
        ]
        public int GrabStayRevTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("投掷预备计时器"), ShowInInspector, ReadOnly]
        public int ThrowStartTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("投掷后跳计时器"), ShowInInspector, ReadOnly]
        public int ThrowMoveTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("拉动冲刺计时器"), ShowInInspector, ReadOnly]
        public int DashTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("拉动冲刺计时器"), ShowInInspector, ReadOnly]
        public int DashWaitTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("口哨前摇计时器"), ShowInInspector, ReadOnly]
        public int WhistleBeforeTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("口哨前摇计时器"), ShowInInspector, ReadOnly]
        public int WhistleStayTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("口哨后摇计时器"), ShowInInspector, ReadOnly]
        public int WhistleAfterTimer { get; set; }

        [BoxGroup("角色运行计时器"), Tooltip("抓跳后再抓cd计时器"), ShowInInspector, ReadOnly]
        public int CanGrabCDTimer { get; set; }

        #endregion

        #region 死亡与重生相关
        [BoxGroup("死亡与重生相关"), Tooltip("死亡判断"), ShowInInspector, ReadOnly]
        public bool isShouldDie { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("重生位置"), ShowInInspector, ReadOnly]
        public Vector3 RespawnPos { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("重生位置偏移"), ShowInInspector, OdinSerialize]
        public Vector3 RespawnOffset { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡后退速度"), ShowInInspector, OdinSerialize]
        public Vector2 DyingBackVelocity { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡黑屏"), ShowInInspector, OdinSerialize]
        public GameObject RespawnBlackMask { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡前效果时间"), ShowInInspector, OdinSerialize]
        public int DyingBeforeTime { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡前效果时间计时器"), ShowInInspector, ReadOnly]
        public int DyingBeforeTimer { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡动画/过度时间"), ShowInInspector, OdinSerialize]
        public int DyingTime { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡动画/过度时间计时器"), ShowInInspector, ReadOnly]
        public int DyingTimer { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡持续时间"), ShowInInspector, OdinSerialize]
        public int DeathTime { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡持续时间计时器"), ShowInInspector, ReadOnly]
        public int DeathTimer { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡持续时间"), ShowInInspector, OdinSerialize]
        public int RespawnTime { get; set; }

        [BoxGroup("死亡与重生相关"), Tooltip("死亡持续时间计时器"), ShowInInspector, ReadOnly]
        public int RespawnTimer { get; set; }

        #endregion

        #region 重力相关

        [BoxGroup("重力相关"), Tooltip("角色的正常最大下落速度"), ShowInInspector, OdinSerialize]
        public float MaxFallSpeedY { get; set; }

        [
            BoxGroup("重力相关"),
            Tooltip("正常的下落加速度（单位：/帧）"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float GravityAccY { get; set; }

        [BoxGroup("重力相关"), Tooltip("缓速下落的速度区间"), ShowInInspector, OdinSerialize]
        public float LowGravThresholdSpeedY { get; set; }

        [BoxGroup("重力相关"), Tooltip("角色缓速下落的加速度倍率"), ShowInInspector, OdinSerialize]
        public float LowGravMult { get; set; }

        [BoxGroup("重力相关"), Tooltip("滑落的最大速度"), ShowInInspector, OdinSerialize]
        public float SlideFallSpeedY { get; set; }
        #endregion

        #region 水平方向移动相关

        [
            BoxGroup("水平方向移动相关"),
            Tooltip("正常水平方向的最大速度"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float MaxSpeedX { get; set; }

        [
            BoxGroup("水平方向移动相关"),
            Tooltip("水平方向上的加速度（单位：/帧）"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float AccX { get; set; }

        [
            BoxGroup("水平方向移动相关"),
            Tooltip("角色水平方向上超速的减速度（单位：/帧）"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float OverReduceX { get; set; }

        [
            BoxGroup("水平方向移动相关"),
            Tooltip("角色在空中的加速度倍率"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float AirAccMultX { get; set; }
        #endregion

        #region 跳跃相关

        [BoxGroup("跳跃相关"), Tooltip("角色普通跳跃的向上的速度"), ShowInInspector, OdinSerialize]
        public float JumpSpeedY { get; set; }

        [
            BoxGroup("跳跃相关"),
            Tooltip("角色起跳时水平方向获得的额外速度倍率"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float JumpBoostMultX { get; set; }

        [
            BoxGroup("跳跃相关"),
            Tooltip("角色起跳时水平方向获得的额外速度"),
            ShowInInspector,
            ReadOnly,
        ]
        public float JumpBoostSpeedX => JumpBoostMultX * MaxSpeedX;

        [
            BoxGroup("跳跃相关"),
            Tooltip("角色最少的跳跃时长（单位：帧）"),
            ShowInInspector,
            OdinSerialize,
        ]
        public int MinJumpTime { get; set; }

        [
            BoxGroup("跳跃相关"),
            Tooltip("角色最长的跳跃时长（单位：帧）"),
            ShowInInspector,
            OdinSerialize,
        ]
        public int MaxJumpTime { get; set; }

        [BoxGroup("跳跃相关"), Tooltip("角色预输入跳跃的时间窗口"), ShowInInspector, OdinSerialize]
        public int PreJumpInputTime { get; set; }

        [BoxGroup("跳跃相关"), Tooltip("角色土狼跳的输入窗口"), ShowInInspector, OdinSerialize]
        public int CoyoteJumpInputTime { get; set; }
        #endregion

        #region 反墙相关
        [BoxGroup("反墙相关"), Tooltip("靠墙判定的范围x"), ReadOnly, OdinSerialize]
        public bool LeftSlideCheck { get; set; }

        [BoxGroup("反墙相关"), Tooltip("靠墙判定的范围x"), ReadOnly, OdinSerialize]
        public bool RightSlideCheck { get; set; }

        [BoxGroup("反墙相关"), Tooltip("反墙的方向"), ReadOnly, OdinSerialize]
        public int WallDir { get; set; }

        [BoxGroup("反墙相关"), Tooltip("靠墙判定的范围x"), ShowInInspector, OdinSerialize]
        public float ByWallCheckDistanceX { get; set; }

        [BoxGroup("反墙相关"), Tooltip("反墙跳强制朝向时间"), ShowInInspector, OdinSerialize]
        public int WallJumpForceTime { get; set; }

        [BoxGroup("反墙相关"), Tooltip("反墙跳土狼时间窗口"), ShowInInspector, OdinSerialize]
        public int WallCoyoteJumpInputTime { get; set; }

        #endregion

        #region 抓住相关
        [BoxGroup("抓住相关"), Tooltip("是否允许更宽松的抓住判定"), ShowInInspector, OdinSerialize]
        public bool IsBroadGrab { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓跳后再抓cd计时"), ShowInInspector, OdinSerialize]
        public int CanGrabCDTime { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓住暂留的时长"), ShowInInspector, OdinSerialize]
        public int GrabStayTime { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓住速度阈值"), ShowInInspector, OdinSerialize]
        public int GrabThresholdSpeedY { get; set; }

        [
            BoxGroup("抓住相关"),
            Tooltip("抓住拐角的手部偏移(基于头顶)y"),
            ShowInInspector,
            OdinSerialize
        ]
        public float CornerGrabStartOffsetY { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓住拐角的范围y"), ShowInInspector, OdinSerialize]
        public float CornerGrabOffsetY { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓住拐角的范围x"), ShowInInspector, OdinSerialize]
        public float CornerGrabOffsetX { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓住的范围x"), ShowInInspector, OdinSerialize]
        public float GrabRangeX { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓住的范围y"), ShowInInspector, OdinSerialize]
        public float GrabRangeY { get; set; }

        [BoxGroup("抓住相关"), Tooltip("抓住的范围的起点偏移点"), ShowInInspector, OdinSerialize]
        public Vector2 GrabRangeOffset { get; set; }

        #endregion

        #region 口哨相关
        [BoxGroup("口哨相关"), Tooltip("口哨预输入时间"), ShowInInspector, OdinSerialize]
        public int PreWhistleInputTime { get; set; }

        [BoxGroup("口哨相关"), Tooltip("口哨前摇时间"), ShowInInspector, OdinSerialize]
        public int WhistleBeforeTime { get; set; }

        [BoxGroup("口哨相关"), Tooltip("口哨最小维持时间"), ShowInInspector, OdinSerialize]
        public int WhistleStayTime { get; set; }

        [BoxGroup("口哨相关"), Tooltip("口哨后摇时间"), ShowInInspector, OdinSerialize]
        public int WhistleAfterTime { get; set; }

        [BoxGroup("口哨相关"), Tooltip("口哨后摇结束确认变量"), ShowInInspector, ReadOnly]
        public bool IsInputEnd { get; set; }

        #endregion

        #region 投掷与拉动相关
        [BoxGroup("投掷与拉动相关"), Tooltip("预输入投掷的时间"), ShowInInspector, OdinSerialize]
        public int PreThrowTime { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("投掷未命中的cd"), ShowInInspector, OdinSerialize]
        public int ThrowCdTime { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("投掷预备时间"), ShowInInspector, OdinSerialize]
        public int ThrowStartTime { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("投掷后跳时间"), ShowInInspector, OdinSerialize]
        public int ThrowMoveTime { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("投掷后跳速度"), ShowInInspector, OdinSerialize]
        public Vector2 ThrowMoveVelocity { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("投掷物速度"), ShowInInspector, OdinSerialize]
        public Vector2 ThrowHookVelocity { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("拉动冲刺的速度"), ShowInInspector, OdinSerialize]
        public float DashSpeed { get; set; }

        [
            BoxGroup("投掷与拉动相关"),
            Tooltip("拉动冲刺前的暂停时间"),
            ShowInInspector,
            OdinSerialize
        ]
        public int DashWaitTime { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("拉动冲刺的时间"), ShowInInspector, OdinSerialize]
        public int DashTime { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("冲刺末段减速的时间"), ShowInInspector, OdinSerialize]
        public int DashEndSlowTime { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("减速的倍率"), ShowInInspector, OdinSerialize]
        public float EndSlowMult { get; set; }

        [BoxGroup("投掷与拉动相关"), Tooltip("投掷起始位置的偏移"), ShowInInspector, OdinSerialize]
        public Vector3 ThrowStartOffset { get; set; }
        #endregion
    }
}
