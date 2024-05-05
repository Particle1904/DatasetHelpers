using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class JoyTagInputData
    {
        [ColumnName("input_1:0")]
        [VectorType(1, 3, 448, 448)]
        public DenseTensor<float>? Input { get; set; }
    }
}