using Microsoft.ML.Data;

namespace SmartData.Lib.Models.MachineLearning
{
    public class JoyTagInputData
    {
        [ColumnName("input_1:0")]
        [VectorType(1, 3, 448, 448)]
        public float[]? Input1 { get; set; }
    }
}