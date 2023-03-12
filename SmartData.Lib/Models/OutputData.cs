using Microsoft.ML.Data;

namespace DatasetHelpers.Models
{
    public class OutputData
    {
        [ColumnName("predictions_sigmoid")]
        public VBuffer<float> PredictionsSigmoid { get; set; }
    }
}
