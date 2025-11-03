namespace CharacterControllerDemo
{
    // csharpier-ignore-start
    public enum PlayerMovementTickOrder
    {
        AccelerateOnGround                      ,
        InverseAccelerateOnGround               ,
        DeAccelerateOnGround                    ,
        AccelerateOnAir                         ,
        InverseAccelerateOnAir                  ,
        DeAccelerateOnAir                       ,
        AccelerateWhileOverspeedOnGround        ,
        InverseAccelerateWhileOverspeedOnGround ,
        DeAccelerateWhileOverspeedOnGround      ,
        AccelerateWhileOverspeedOnAir           ,
        InverseAccelerateWhileOverspeedOnAir    ,
        DeAccelerateWhileOverspeedOnAir         ,
        Gravity                                 ,
        Jump                                    ,
        JumpApexModifiers                       ,
        JumpBuffering                           ,
        CoyoteTime                              ,
        ClampedFallSpeed                        ,
        EdgeSliding                             ,
        JumpExtraSpeed                          ,
    }
    // csharpier-ignore-end
}
