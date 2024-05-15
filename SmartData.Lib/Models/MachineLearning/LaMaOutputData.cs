using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Models.MachineLearning
{
    public class LaMaOutputData
    {
        [ColumnName("output")]
        [VectorType(1, 3, 512, 512)]
        public DenseTensor<float>? OutputImage { get; set; }

        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
    }
}
