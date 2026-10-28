using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class WDInputData
    {
        public DenseTensor<float> Input { get; set; }
    }
}
