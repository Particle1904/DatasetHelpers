using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class UpscalerInputData
    {
        [ColumnName("input")]
        [VectorType(1, 3, 0, 0)]
        public DenseTensor<float>? Input { get; set; }
    }
}