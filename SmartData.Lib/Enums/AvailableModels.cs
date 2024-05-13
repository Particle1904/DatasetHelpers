namespace SmartData.Lib.Enums
{
    public enum AvailableModels : byte
    {
        #region Tag Generators
        JoyTag,
        WD14v2,
        WDv3,
        Z3DE621,
        #endregion
        #region Yolo models
        Yolov4,
        #endregion
        #region 2x Upscalers
        ParimgCompactx2,
        HFA2kCompactx2,
        HFA2kAVCSRFormerLightx2,
        #endregion
        #region 4x Upscalers
        HFA2kx4,
        SwinIRx4,
        Swin2SRx4,
        Nomos8kSCSRFormerx4,
        Nomos8kSCx4,
        LSDIRplusRealx4,
        LSDIRplusNonex4,
        LSDIRplusCompressionx4,
        LSDIRCompact3x4,
        LSDIRx4,
        #endregion
        CLIPTokenizer
    }
}
