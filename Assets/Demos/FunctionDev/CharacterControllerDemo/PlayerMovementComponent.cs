using Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        #region 运动状态

        [
            BoxGroup("运动状态"),
            Tooltip("玩家当前移动速度, 与刚体速度同步"),
            ShowInInspector,
            ReadOnly
        ]
        public Vector2 Velocity { get; set; } = Vector2.zero;

        [
            BoxGroup("运动状态"),
            Tooltip("玩家主动移动能到达的 最高速度 (水平方向)"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float MaxVelocityX { get; set; }

        [
            BoxGroup("运动状态"),
            Tooltip("玩家被允许的 最高速度 (水平方向)"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float ClampVelocityX { get; set; }

        [
            BoxGroup("运动状态"),
            Tooltip("玩家被允许的 最高速度 (垂直方向)"),
            ShowInInspector,
            OdinSerialize,
        ]
        public float ClampVelocityY { get; set; }

        #endregion // 运动状态

        #region 重力设置

        [
            BoxGroup("重力设置"),
            Tooltip("作用在玩家身上的 重力 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float GravityAcceleration { get; set; }

        #endregion // 重力设置

        #region 物理设置

        [
            BoxGroup("物理设置"),
            Tooltip("用于检测玩家是否在地面的 射线长度"),
            ShowInInspector,
            OdinSerialize
        ]
        public float GroundCheckDistance { get; set; }

        #endregion // 物理设置

        #region 跳跃设置

        [
            BoxGroup("跳跃设置"),
            Tooltip("玩家 输入跳跃操作时 被赋予的垂直方向速度"),
            ShowInInspector,
            OdinSerialize
        ]
        public float JumpSpeedY { get; set; }

        [
            BoxGroup("跳跃设置"),
            Tooltip("玩家 输入跳跃操作时, 至少会持续MinJumpScaledTime的跳跃"),
            ShowInInspector,
            OdinSerialize
        ]
        public float MinJumpScaledTime { get; set; }

        [
            BoxGroup("跳跃设置"),
            Tooltip("玩家 输入跳跃操作时, 最多会持续MaxJumpScaledTime的跳跃"),
            ShowInInspector,
            OdinSerialize
        ]
        public float MaxJumpScaledTime { get; set; }

        #endregion // 跳跃设置

        #region 跳跃设置-Jump Extra Speed

        [
            BoxGroup("跳跃设置-Jump Extra Speed"),
            Tooltip(
                "玩家 输入跳跃操作时 如果有水平输入操作 则给予一个额外的水平速度来减少到达最高速度的时间"
            ),
            ShowInInspector,
            OdinSerialize
        ]
        public float JumpExtraSpeedX { get; set; }

        #endregion // 跳跃设置-Jump Extra Speed

        #region 跳跃设置-Jump Buffering
#if UNITY_EDITOR

        [
            BoxGroup("跳跃设置-Jump Buffering"),
            Tooltip(
                "玩家 输入跳跃操作后 如果还没有到达地面, 则缓存这个跳跃指令一段时间, 在玩家到达地面时会立刻执行跳跃以增强动作连续性\n 这个值应该在VgInput系统中设置"
            ),
            ShowInInspector,
            OdinSerialize,
            ReadOnly
        ]
        public float JumpBufferingUnscaledTime { get; set; } = 0.2f;

#endif // UNITY_EDITOR
        #endregion // 跳跃设置-Jump Buffering

        #region 跳跃设置-Coyote Time

        [
            BoxGroup("跳跃设置-Coyote Time"),
            Tooltip(
                "玩家 输入跳跃操作后 如果已经不在地面, 则给予一个短暂时间窗口, 在窗口内起跳仍然能够成功"
            ),
            ShowInInspector,
            OdinSerialize
        ]
        public float CoyoteJumpScaledTime { get; set; }

        #endregion // 跳跃设置-Coyote Time

        #region 跳跃设置-Apex Modifiers

        [
            BoxGroup("跳跃设置-Apex Modifiers"),
            Tooltip(
                "玩家 输入跳跃操作后 在跳跃的最高点时 有一个短暂的窗口, 这个窗口会减少重力, 并轻微提升最高速度"
            ),
            ShowInInspector,
            OdinSerialize
        ]
        public float JumpApexModifiersScaledTime { get; set; }

        [
            BoxGroup("跳跃设置-Apex Modifiers"),
            Tooltip("窗口期间内的 重力 值 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float JumpApexGravityValue { get; set; }

        [
            BoxGroup("跳跃设置-Apex Modifiers"),
            Tooltip("窗口期间内的 最高速度的 增加值 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float JumpApexSpeedLimitModifier { get; set; }

        #endregion // 跳跃设置-Apex Modifiers

        #region 水平移动设置-地面

        [
            BoxGroup("水平移动设置-地面"),
            Tooltip("玩家在 地面上 输入水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float AccelerationOnGround { get; set; }

        [
            BoxGroup("水平移动设置-地面"),
            Tooltip("玩家在 地面上 输入逆向水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float InverseAccelerationOnGround { get; set; }

        [
            BoxGroup("水平移动设置-地面"),
            Tooltip("玩家在 地面上 没有水平移动操作 时的 减速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float DeAccelerationOnGround { get; set; }

        #endregion // 水平移动设置-地面

        #region 水平移动设置-天空

        [
            BoxGroup("水平移动设置-天空"),
            Tooltip("玩家在 天空中 输入水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float AccelerationOnAir { get; set; }

        [
            BoxGroup("水平移动设置-天空"),
            Tooltip("玩家在 天空中 输入逆向水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float InverseAccelerationOnAir { get; set; }

        [
            BoxGroup("水平移动设置-天空"),
            Tooltip("玩家在 天空中 没有水平移动操作 时的 减速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float DeAccelerationOnAir { get; set; }

        #endregion // 水平移动设置-天空

        #region  水平移动设置-地面-超速

        [
            BoxGroup("水平移动设置-地面-超速"),
            Tooltip("玩家在 地面上 超速时 输入水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float AccelerationWhileOverspeedOnGround { get; set; }

        [
            BoxGroup("水平移动设置-地面-超速"),
            Tooltip("玩家在 地面上 超速时 输入逆向水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float InverseAccelerationWhileOverspeedOnGround { get; set; }

        [
            BoxGroup("水平移动设置-地面-超速"),
            Tooltip("玩家在 地面上 超速时 没有水平移动操作 时的 减速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float DeAccelerationWhileOverspeedOnGround { get; set; }

        #endregion // 水平移动设置-地面-超速

        #region 水平移动设置-天空-超速

        [
            BoxGroup("水平移动设置-天空-超速"),
            Tooltip("玩家在 天空中 超速时 输入水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float AccelerationWhileOverspeedOnAir { get; set; }

        [
            BoxGroup("水平移动设置-天空-超速"),
            Tooltip("玩家在 天空中 超速时 输入逆向水平移动操作 时的 加速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float InverseAccelerationWhileOverspeedOnAir { get; set; }

        [
            BoxGroup("水平移动设置-天空-超速"),
            Tooltip("玩家在 天空中 超速时 没有水平移动操作 时的 减速度 (正值)"),
            ShowInInspector,
            OdinSerialize
        ]
        public float DeAccelerationWhileOverspeedOnAir { get; set; }

        #endregion // 水平移动设置-天空-超速

        #region 辅助值状态
        [BoxGroup("辅助值状态"), Tooltip("玩家当前移动状态"), ShowInInspector, ReadOnly]
        public PlayerMoveState CurrentMovementState { get; set; }

        [BoxGroup("辅助值状态"), Tooltip("玩家是否在地面上"), ShowInInspector, ReadOnly]
        public bool IsOnGround { get; set; }

        [BoxGroup("辅助值状态"), Tooltip("玩家是否顶到天花板"), ShowInInspector, ReadOnly]
        public bool IsUnderCeiling { get; set; }

        [BoxGroup("辅助值状态"), Tooltip("玩家待在 地面上 的连续时长"), ShowInInspector, ReadOnly]
        public float ScaledTimeOnGround { get; set; }

        [BoxGroup("辅助值状态"), Tooltip("玩家待在 天空中 的连续时长"), ShowInInspector, ReadOnly]
        public float ScaledTimeInAir { get; set; }

        [
            BoxGroup("辅助值状态"),
            Tooltip(
                "玩家 从跳跃开始 到未达到跳跃最高时间时 的时长\n(人话: 已经跳跃了多久, 但是这个时间不会超过最长跳跃时间)"
            ),
            ShowInInspector,
            ReadOnly,
        ]
        public float JumpScaledTimeSinceStarted { get; set; }

        [
            BoxGroup("辅助值状态"),
            Tooltip(
                "玩家 进入跳跃顶点修改窗口 到未达到窗口结束时间时 的时长\n(人话: 已经进入跳跃顶点修改窗口了多久, 但是这个时间不会超过最长窗口时间)"
            ),
            ShowInInspector,
            ReadOnly,
        ]
        public float JumpApexModifierScaledTimeSinceStarted { get; set; }

        #endregion // 辅助值状态
    }
}
