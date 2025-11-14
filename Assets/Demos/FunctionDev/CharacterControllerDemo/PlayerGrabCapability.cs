using System.Collections.Generic;
using CharacterControllerDemo;
using Core;
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
    protected override bool OnShouldActivate()
    {
        if (!VgInput.GetButton(InputAction.Grab))
            return false;

        if (!mPlayerMovementComponent.IsFalling())
            return false;

        // if (!mPlayerMovementComponent.IsInGrabZone())
        //     return false;

        return true;
    }

    protected override bool OnShouldDeactivate()
    {
        return false;
    }

    protected override void OnTick(float deltaTime)
    {
        // Do nothing
    }
}

public class PlayerGrabExitCapability : PlayerGrabCapabilityBase
{
    protected override bool OnShouldActivate()
    {
        return false;
    }

    protected override bool OnShouldDeactivate()
    {
        return true;
    }

    protected override void OnTick(float deltaTime)
    {
        // Do nothing
    }
}
