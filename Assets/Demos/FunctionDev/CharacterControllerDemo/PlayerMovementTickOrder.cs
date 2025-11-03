namespace CharacterControllerDemo
{
    // csharpier-ignore-start
    public static class PlayerMovementTickOrder
    {
        public const int AccelerateOnGround                      = 0;
        public const int InverseAccelerateOnGround               = 1;
        public const int DeAccelerateOnGround                    = 2;
        public const int AccelerateOnAir                         = 3;
        public const int InverseAccelerateOnAir                  = 4;
        public const int DeAccelerateOnAir                       = 5;
        public const int AccelerateWhileOverspeedOnGround        = 6;
        public const int InverseAccelerateWhileOverspeedOnGround = 7;
        public const int DeAccelerateWhileOverspeedOnGround      = 8;
        public const int AccelerateWhileOverspeedOnAir           = 9;
        public const int InverseAccelerateWhileOverspeedOnAir    = 10;
        public const int DeAccelerateWhileOverspeedOnAir         = 11;
        public const int Gravity                                 = 12;
        public const int Jump                                    = 13;
        public const int JumpApexModifiers                       = 14;
        public const int JumpBuffering                           = 15;
        public const int CoyoteTime                              = 16;
        public const int ClampedFallSpeed                        = 17;
        public const int EdgeSliding                             = 18;
        public const int JumpExtraSpeed                          = 19;
    }
    // csharpier-ignore-end
}
