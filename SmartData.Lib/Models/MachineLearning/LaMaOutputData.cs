using Microsoft.ML.OnnxRuntime.Tensors;

namespace Models.MachineLearning
{
    public class LaMaOutputData
    {
        public DenseTensor<float>? OutputImage { get; set; }

        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
    }
}
