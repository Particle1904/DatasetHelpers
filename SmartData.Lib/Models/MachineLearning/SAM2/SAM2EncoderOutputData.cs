using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2EncoderOutputData
    {
        [ColumnName("high_res_feats_0")]
        [VectorType(0, 0, 0, 0)]
        public DenseTensor<float>? HighResFeats0 { get; set; }

        [ColumnName("high_res_feats_1")]
        [VectorType(0, 0, 0, 0)]
        public DenseTensor<float>? HighResFeats1 { get; set; }

        [ColumnName("image_embed")]
        [VectorType(0, 0, 0, 0)]
        public DenseTensor<float>? ImageEmbed { get; set; }
    }
}
