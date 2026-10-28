using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2EncoderInputData
    {
        public DenseTensor<float>? InputImage { get; set; }
    }
}
