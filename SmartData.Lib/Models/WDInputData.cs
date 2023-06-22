using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class WDInputData
    {
        [ColumnName("input_1:0")]
        [VectorType(1, 448, 448, 3)]
        public float[]? Input1 { get; set; }
    }
}
