using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

using System.Drawing;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2DecoderOutputData
    {
        [ColumnName("masks")]
        [VectorType(0, 0, 0, 0)]
        public DenseTensor<float>? Masks { get; set; }

        [ColumnName("iou_predictions")]
        [VectorType(0, 0)]
        public DenseTensor<float>? IouPredictions { get; set; }

        public Size OriginalResolution { get; set; }
    }
}
