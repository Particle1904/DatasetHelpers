using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class UpscalerInputData
    {
        public DenseTensor<float>? Input { get; set; }
    }
}