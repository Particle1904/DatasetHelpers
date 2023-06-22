using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class BLIPInputData
    {
        [ColumnName("input_ids")]
        [VectorType(1, 128)]
        public long[,]? InputIds { get; set; }

        [ColumnName("pixel_values")]
        [VectorType(1, 3, 384, 384)]
        public float[]? PixelValues { get; set; }
    }
}
