using Microsoft.ML.OnnxRuntime.Tensors;

using System.Drawing;

namespace SmartData.Lib.Models.MachineLearning.SAM2
{
    public class SAM2DecoderOutputData
    {
        public DenseTensor<float>? Masks { get; set; }
        public DenseTensor<float>? IouPredictions { get; set; }
        
        public Size OriginalResolution { get; set; }
    }
}
