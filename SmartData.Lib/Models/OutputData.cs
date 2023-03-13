using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class OutputData
    {
        [ColumnName("predictions_sigmoid")]
        public VBuffer<float> PredictionsSigmoid { get; set; }
    }
}
