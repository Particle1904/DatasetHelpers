using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class InputData
    {
        [ColumnName("input_1:0")]
        [VectorType(1, 448, 448, 3)]
        public float[]? Input_1 { get; set; }
    }
}
