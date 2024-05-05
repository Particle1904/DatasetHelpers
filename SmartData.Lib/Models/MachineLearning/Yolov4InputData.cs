using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class Yolov4InputData
    {
        [ColumnName("input_1:0")]
        [VectorType(1, 416, 416, 3)]
        public DenseTensor<float> Input { get; set; }
    }
}
