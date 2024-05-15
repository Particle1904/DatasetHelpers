using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;

namespace Models.MachineLearning
{
    public class LaMaInputData
    {
        [ColumnName("image")]
        [VectorType(1, 3, 512, 512)]
        public DenseTensor<float>? InputImage { get; set; }

        [ColumnName("mask")]
        [VectorType(1, 1, 512, 512)]
        public DenseTensor<float>? InputMask { get; set; }

        public Point OriginalSize { get; set; }
    }
}
