using System.Collections;
using System.Collections.Generic;
using CharacterControllerDemo;
using UnityEngine;
using VanishingGames.ECC.Runtime;

public class PlayerGroundCheckCapability : PlayerMoveCapability
{
    protected override void OnSetup()
    {
        base.OnSetup();
        TickOrderInGroup = (uint)PlayerMovementTickOrder.GroundCheck;
        Tags = new List<EccTag> { EccTag.GroundCheck };
    }

    protected override bool OnShouldActivate()
    {
        return true;
    }

    protected override bool OnShouldDeactivate()
    {
        return false;
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        mPlayerMovementComponent.ScaledTimeOnGround = 0f;
        mPlayerMovementComponent.ScaledTimeInAir = 0f;
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        mPlayerMovementComponent.ScaledTimeOnGround = 0f;
        mPlayerMovementComponent.ScaledTimeInAir = 0f;
    }

    protected override void OnTick(float deltaTime)
    {
        var lastFrameOnGround = mPlayerMovementComponent.IsOnGround;

        mPlayerMovementComponent.UpdateUnderCeilingStatus();
        mPlayerMovementComponent.UpdateOnGroundStatus();

        var thisFrameOnGround = mPlayerMovementComponent.IsOnGround;

        if (thisFrameOnGround)
        {
            mPlayerMovementComponent.ScaledTimeOnGround += deltaTime;
            mPlayerMovementComponent.ScaledTimeInAir = 0f;

            if (mPlayerMovementComponent.JumpScaledTimeSinceStarted != 0f) // 防止已经起跳, 但由于地面检测误差导致的状态错误更改
                mPlayerMovementComponent.CurrentMovementState = PlayerMoveState.OnGround;

            if (!lastFrameOnGround)
                mPlayerMovementComponent.OnPlayerLandedEvent.OnNext(0);
        }
        else
        {
            mPlayerMovementComponent.ScaledTimeOnGround = 0f;
            mPlayerMovementComponent.ScaledTimeInAir += deltaTime;

            if (lastFrameOnGround)
                mPlayerMovementComponent.OnPlayerLeaveGroundEvent.OnNext(0);
        }
    }
}
