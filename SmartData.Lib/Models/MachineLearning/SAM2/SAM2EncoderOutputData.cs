using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2EncoderOutputData
    {
        public DenseTensor<float>? HighResFeats0 { get; set; }
        public DenseTensor<float>? HighResFeats1 { get; set; }
        public DenseTensor<float>? ImageEmbed { get; set; }
    }
}
