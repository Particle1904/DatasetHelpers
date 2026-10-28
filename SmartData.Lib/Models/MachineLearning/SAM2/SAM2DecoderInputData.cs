using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2DecoderInputData
    {
        public DenseTensor<float>? ImageEmbed { get; set; }
        public DenseTensor<float>? HighResFeats0 { get; set; }
        public DenseTensor<float>? HighResFeats1 { get; set; }
        public DenseTensor<float>? PointCoords { get; set; }
        public DenseTensor<float>? PointLabels { get; set; }
        public DenseTensor<float>? MaskInput { get; set; }
        public DenseTensor<float>? HasMaskInput { get; set; }
    }
}
