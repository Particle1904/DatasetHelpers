using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class BLIPOutputData
    {
        [VectorType(1, 1)]
        [ColumnName("output")]
        public float[] output { get; set; }
    }
}
