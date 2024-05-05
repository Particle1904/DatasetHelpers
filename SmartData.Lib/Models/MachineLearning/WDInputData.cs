using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class WDInputData
    {
        [ColumnName("input_1:0")]
        [VectorType(1, 448, 448, 3)]
        public DenseTensor<float> Input { get; set; }
    }
}
