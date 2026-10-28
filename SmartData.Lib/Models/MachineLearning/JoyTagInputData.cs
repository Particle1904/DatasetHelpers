using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class JoyTagInputData
    {
        public DenseTensor<float>? Input { get; set; }
    }
}