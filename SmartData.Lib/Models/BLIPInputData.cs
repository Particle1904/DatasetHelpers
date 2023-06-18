using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class BLIPInputData
    {
        [ColumnName("input_ids")]
        [VectorType(1, 512)]
        public long[] Input_Ids { get; set; } = new long[512];

        [ColumnName("pixel_values")]
        [VectorType(1, 3, 384, 384)]
        public float[]? pixel_values { get; set; }
    }
}
