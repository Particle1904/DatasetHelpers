using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class Yolov4InputData
    {
        [ColumnName("input_1:0")]
        [VectorType(1, 416, 416, 3)]
        public float[]? Input1 { get; set; }
    }
}
