using SmartData.Lib.Enums;
using SmartData.Lib.Models.ModelManager;

namespace Models.ModelManager
{
    public static class ModelRegistry
    {
        public static readonly IReadOnlyDictionary<AvailableModels, (ModelFileInfo Model, ModelFileInfo? Csv)> RequiredFiles =
            new Dictionary<AvailableModels, (ModelFileInfo, ModelFileInfo?)>
            {
                #region Tag Generators
                [AvailableModels.WD14v2] = (
                    new ModelFileInfo
                    {
                        Filename = "wdModel.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdModel.onnx?download=true"
                    },
                    new ModelFileInfo
                    {
                        Filename = "wdTags.csv",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdTags.csv?download=true"
                    }),

                [AvailableModels.WDv3] = (
                    new ModelFileInfo
                    {
                        Filename = "wdV3Model.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdV3Model.onnx?download=true"
                    },
                    new ModelFileInfo
                    {
                        Filename = "wdV3Tags.csv",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdV3Tags.csv?download=true"
                    }),

                [AvailableModels.WDv3Large] = (
                    new ModelFileInfo
                    {
                        Filename = "wdV3LargeModel.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdV3LargeModel.onnx?download=true"
                    },
                    new ModelFileInfo
                    {
                        Filename = "wdV3LargeTags.csv",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdV3LargeTags.csv?download=true"
                    }
                ),

                [AvailableModels.JoyTag] = (
                    new ModelFileInfo
                    {
                        Filename = "jtModel.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/jtModel.onnx?download=true"
                    },
                    new ModelFileInfo
                    {
                        Filename = "jtTags.csv",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/jtTags.csv?download=true"
                    }),

                [AvailableModels.Z3DE621] = (
                    new ModelFileInfo
                    {
                        Filename = "e621Model.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/e621Model.onnx?download=true"
                    },
                    new ModelFileInfo
                    {
                        Filename = "e621Tags.csv",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/e621Tags.csv?download=true"
                    }),
                #endregion
                #region Yolo
                [AvailableModels.Yolov4] = (
                    new ModelFileInfo
                    {
                        Filename = "yolov4.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/yolov4.onnx?download=true"
                    },
                    null),
                #endregion
                #region Others/Onnx Extensions
                [AvailableModels.CLIPTokenizer] = (
                    new ModelFileInfo
                    {
                        Filename = "cliptokenizer.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/cliptokenizer.onnx?download=true"
                    },
                    null),
                #endregion
                #region Upscalers
                [AvailableModels.ParimgCompact_x2] = (
                    new ModelFileInfo
                    {
                        Filename = "ParimgCompact.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/ParimgCompact.onnx?download=true"
                    },
                    null),
                [AvailableModels.SwinIR_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "swinIR.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/swinIR.onnx?download=true"
                    },
                    null),
                [AvailableModels.Swin2SR_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "swin2SR.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/swin2SR.onnx?download=true"
                    },
                    null),
                [AvailableModels.HFA2kCompact_x2] = (
                    new ModelFileInfo
                    {
                        Filename = "HFA2kCompact.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/HFA2kCompact.onnx?download=true"
                    },
                    null),
                [AvailableModels.HFA2kAVCSRFormerLight_x2] = (
                    new ModelFileInfo
                    {
                        Filename = "HFA2kAVCSRFormerLight.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/HFA2kAVCSRFormerLight.onnx?download=true"
                    },
                    null),
                [AvailableModels.HFA2k_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "HFA2k.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/HFA2k.onnx?download=true"
                    },
                    null),
                [AvailableModels.Nomos8kSCSRFormer_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "Nomos8kSCSRFormer.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/Nomos8kSCSRFormer.onnx?download=true"
                    },
                    null),
                [AvailableModels.Nomos8kSC_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "Nomos8kSC.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/Nomos8kSC.onnx?download=true"
                    },
                    null),
                [AvailableModels.LSDIRplusReal_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "LSDIRplusReal.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRplusReal.onnx?download=true"
                    },
                    null),
                [AvailableModels.LSDIRplusNone_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "LSDIRplusNone.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRplusNone.onnx?download=true"
                    },
                    null),
                [AvailableModels.LSDIRplusCompression_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "LSDIRplusCompression.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRplusCompression.onnx?download=true"
                    },
                    null),
                [AvailableModels.LSDIRCompact3_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "LSDIRCompact3.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRCompact3.onnx?download=true"
                    },
                    null),
                [AvailableModels.LSDIR_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "LSDIR.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIR.onnx?download=true"
                    },
                    null),
                [AvailableModels.Nomos8k_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "Nomos8k.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/Nomos8k.onnx?download=true"
                    },
                    null),
                [AvailableModels.Nomos8kDAT_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "Nomos8kDAT.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/Nomos8kDAT.onnx?download=true"
                    },
                    null),
                [AvailableModels.NomosUni_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "NomosUni.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/NomosUni.onnx?download=true"
                    },
                    null),
                [AvailableModels.RealWebPhoto_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "RealWebPhoto.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/RealWebPhoto.onnx?download=true"
                    },
                    null),
                [AvailableModels.RealWebPhotoDAT_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "RealWebPhotoDAT.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/RealWebPhotoDAT.onnx?download=true"
                    },
                    null),
                [AvailableModels.SPANkendata_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "SPANkendata.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/SPANkendata.onnx?download=true"
                    },
                    null),
                [AvailableModels.GTAV5_x4] = (
                    new ModelFileInfo
                    {
                        Filename = "GTAV.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/GTAV.onnx?download=true"
                    },
                    null),
                #endregion
                #region Inpaint Service
                [AvailableModels.LaMa] = (
                    new ModelFileInfo
                    {
                        Filename = "LaMa.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/LaMa.onnx?download=true"
                    },
                    null),
                #endregion
                #region SAM2 Service
                [AvailableModels.SAM2Encoder] = (
                    new ModelFileInfo
                    {
                        Filename = "sam2HieraBaseEncoder.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/sam2HieraBaseEncoder.onnx?download=true"
                    },
                    null),
                [AvailableModels.SAM2Decoder] = (
                    new ModelFileInfo
                    {
                        Filename = "sam2HieraBaseDecoder.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/sam2HieraBaseDecoder.onnx?download=true"
                    },
                    null),
                #endregion
                #region Florence2 Service
                [AvailableModels.Florence2VisionEncoder] = (
                    new ModelFileInfo
                    {
                        Filename = "florence2VisionEncoder.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/florence2VisionEncoder.onnx?download=true"
                    },
                    null),
                [AvailableModels.Florence2Encoder] = (
                    new ModelFileInfo
                    {
                        Filename = "florence2Encoder.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/florence2Encoder.onnx?download=true"
                    },
                    null),
                [AvailableModels.Florence2Decoder] = (
                    new ModelFileInfo
                    {
                        Filename = "florence2Decoder.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/florence2Decoder.onnx?download=true"
                    },
                    null),
                [AvailableModels.Florence2EmbedTokens] = (
                    new ModelFileInfo
                    {
                        Filename = "florence2EmbedTokens.onnx",
                        DownloadUrl = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/florence2EmbedTokens.onnx?download=true"
                    },
                    null)
                #endregion
            };
    }
}
