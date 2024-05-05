using Microsoft.ML.Data;

namespace SmartData.Lib.Models.MachineLearning
{
    public class JoyTagOutputData
    {
        [ColumnName("predictions_sigmoid")]
        public float[] PredictionsSigmoid { get; set; }
    }
}
