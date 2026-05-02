using System.Collections.Generic;
using CharacterControllerDemo;
using Core;
using UnityEngine;
using VanishingGames.ECC.Runtime;

public abstract class PlayerGrabCapabilityBase : PlayerMoveCapability
{
    protected sealed override void SetUpTickSettings()
    {
        TickGroup = EccTickGroup.AfterMovement;
        TickType = EccTickType.Fixed;
        TickOrderInGroup = 0;
        Tags = new List<EccTag> { EccTag.Grab };
    }
}

public class PlayerGrabEnterCapability : PlayerGrabCapabilityBase
{
    protected override void OnActivate()
    {
        base.OnActivate();
        mOwner.BlockCapabilities(
            new[]
            {
                EccTag.Move,
                EccTag.Gravity,
                EccTag.Jump,
                EccTag.CollideAndSlide,
                EccTag.GeometricDepenetration,
            },
            this
        );
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
        mOwner.UnblockCapabilities(this);
    }

    protected override bool ShouldActivate()
    {
        if (!VgInput.GetButton(InputAction.Grab))
            return false;

        if (!mPlayerMovementComponent.IsFalling())
            return false;

        if (!mPlayerMovementComponent.GrabCheck_Platform())
            return false;

        return true;
    }

    protected override bool ShouldDeactivate()
    {
        if (VgInput.GetButton(InputAction.Grab))
            return true;

        if (mPlayerMovementComponent.IsFalling())
            return true;

        if (mPlayerMovementComponent.GrabCheck_Platform())
            return true;

        return false;
    }

    protected override void OnTick(float deltaTime)
    {
        mPlayerMovementComponent.Velocity = Vector2.zero;
    }
}

public class PlayerGrabExitCapability : PlayerGrabCapabilityBase
{
    protected override bool ShouldActivate()
    {
        return false;
    }

    protected override bool ShouldDeactivate()
    {
        return true;
    }

    protected override void OnTick(float deltaTime)
    {
        // Do nothing
    }
}
