using Microsoft.ML.Data;

namespace SmartData.Lib.Models.MachineLearning
{
    public class Yolov4OutputData
    {
        [VectorType(1, 52, 52, 3, 85)]
        [ColumnName("Identity:0")]
        public float[] Identity0 { get; set; }

        [VectorType(1, 26, 26, 3, 85)]
        [ColumnName("Identity_1:0")]
        public float[] Identity1 { get; set; }

        [VectorType(1, 13, 13, 3, 85)]
        [ColumnName("Identity_2:0")]
        public float[] Identity2 { get; set; }
    }
}
