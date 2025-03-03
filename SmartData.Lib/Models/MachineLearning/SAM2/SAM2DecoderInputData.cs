using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2DecoderInputData
    {
        [ColumnName("image_embed")]
        [VectorType(1, 256, 64, 64)]
        public DenseTensor<float>? ImageEmbed { get; set; }

        [ColumnName("high_res_feats_0")]
        [VectorType(1, 32, 256, 256)]
        public DenseTensor<float>? HighResFeats0 { get; set; }

        [ColumnName("high_res_feats_1")]
        [VectorType(1, 64, 128, 128)]
        public DenseTensor<float>? HighResFeats1 { get; set; }

        [ColumnName("point_coords")]
        [VectorType(0, 0, 2)]
        public DenseTensor<float>? PointCoords { get; set; }

        [ColumnName("point_labels")]
        [VectorType(0, 0)]
        public DenseTensor<float>? PointLabels { get; set; }

        [ColumnName("mask_input")]
        [VectorType(0, 1, 256, 256)]
        public DenseTensor<float>? MaskInput { get; set; }

        [ColumnName("has_mask_input")]
        [VectorType(0)]
        public DenseTensor<float>? HasMaskInput { get; set; }

        [ColumnName("orig_im_size")]
        [VectorType(2)]
        public DenseTensor<int>? OriginalImageSize { get; set; }
    }
}
