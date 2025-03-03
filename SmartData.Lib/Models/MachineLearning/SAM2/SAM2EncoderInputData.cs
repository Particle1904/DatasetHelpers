using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2EncoderInputData
    {
        [ColumnName("image")]
        [VectorType(1, 3, 1024, 1024)]
        public DenseTensor<float>? InputImage { get; set; }
    }
}
