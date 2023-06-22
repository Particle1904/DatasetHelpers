using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class BLIPOutputData
    {
        [ColumnName("output")]
        public VBuffer<float> output { get; set; }
    }
}
