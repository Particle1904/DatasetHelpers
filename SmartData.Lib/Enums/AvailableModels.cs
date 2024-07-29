namespace SmartData.Lib.Enums
{
    public enum AvailableModels : byte
    {
        #region Tag Generators
        JoyTag,
        WD14v2,
        WDv3,
        WDv3Large,
        Z3DE621,
        #endregion
        #region Yolo models
        Yolov4,
        #endregion
        #region 2x Upscalers
        ParimgCompact_x2,
        HFA2kCompact_x2,
        HFA2kAVCSRFormerLight_x2,
        #endregion
        #region 4x Upscalers
        HFA2k_x4,
        SwinIR_x4,
        Swin2SR_x4,
        Nomos8k_x4,
        Nomos8kDAT_x4,
        NomosUni_x4,
        RealWebPhoto_x4,
        RealWebPhotoDAT_x4,
        SPANkendata_x4,
        Nomos8kSCSRFormer_x4,
        Nomos8kSC_x4,
        LSDIRplusReal_x4,
        LSDIRplusNone_x4,
        LSDIRplusCompression_x4,
        LSDIRCompact3_x4,
        LSDIR_x4,
        GTAV5_x4,
        #endregion
        #region Inpainters
        LaMa,
        #endregion
        CLIPTokenizer
    }
}
