using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class UpscalerOutputData
    {
        public DenseTensor<float> Output { get; set; }
    }
}
