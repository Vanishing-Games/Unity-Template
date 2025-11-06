using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using VanishingGames.ECC.Runtime;

namespace CharacterControllerDemo
{
    public partial class PlayerMovementComponent : EccComponent
    {
        // csharpier-ignore-start
        /// <summary>
        /// 当玩家开始跳跃时触发
        /// </summary>
        [NonSerialized]
        public Subject<uint> OnPlayerStartJumpEvent;

        /// <summary>
        /// 当玩家结束跳跃时触发, 结束原因可能是松开了跳跃键或者达到了最大跳跃时间
        /// </summary>
        [NonSerialized]
        public Subject<uint> OnPlayerEndJumpEvent;

        /// <summary>
        /// 当玩家达到跳跃顶点窗口时触发
        /// </summary>
        [NonSerialized]
        public Subject<uint> OnPlayerEnterJumpApexEvent;

        /// <summary>
        /// 当玩家离开跳跃顶点窗口时触发
        /// </summary>
        [NonSerialized]
        public Subject<uint> OnPlayerLeaveJumpApexEvent;

        /// <summary>
        /// 当玩家开始自由落体时触发, 可能是松开跳跃, 或者离开了跳跃顶点窗口后
        /// </summary>
        [NonSerialized]
        public Subject<uint> OnPlayerStartFreefallEvent;

        /// <summary>
        /// 当玩家着陆时触发, 即从空中状态变为地面状态时
        /// </summary>
        [NonSerialized]
        public Subject<uint> OnPlayerLandedEvent;

        /// <summary>
        /// 当玩家离开地面时触发, 即从地面状态变为空中状态时
        /// </summary>
        [NonSerialized]
        public Subject<uint> OnPlayerLeaveGroundEvent;
        // csharpier-ignore-end

        private partial void InitializeEvents();
    }
}
