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
        Jump                                    ,
        JumpApexModifiers                       ,
        JumpBuffering                           ,
        CoyoteTime                              ,
        ClampedFallSpeed                        ,
        GeometricDepenetration                  ,
        EdgeSliding                             ,
        JumpExtraSpeed                          ,
        Gravity                                 ,
    }
    // csharpier-ignore-end
}
