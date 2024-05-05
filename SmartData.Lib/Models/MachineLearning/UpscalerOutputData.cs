using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class UpscalerOutputData
    {
        [ColumnName("output")]
        [VectorType(1, 3, 0, 0)]
        public DenseTensor<float> Output { get; set; }
    }
}
