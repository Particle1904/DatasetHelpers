using Microsoft.ML.Data;

namespace SmartData.Lib.Models
{
    public class WDOutputData
    {
        [ColumnName("predictions_sigmoid")]
        public VBuffer<float> PredictionsSigmoid { get; set; }
    }
}
