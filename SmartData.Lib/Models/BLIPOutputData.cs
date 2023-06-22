using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class BLIPOutputData
    {
        [ColumnName("output")]
        public float[] Output { get; set; }
    }
}
