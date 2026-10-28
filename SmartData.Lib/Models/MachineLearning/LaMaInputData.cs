using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;

namespace Models.MachineLearning
{
    public class LaMaInputData
    {
        public DenseTensor<float>? InputImage { get; set; }

        public DenseTensor<float>? InputMask { get; set; }

        public Point OriginalSize { get; set; }
    }
}
