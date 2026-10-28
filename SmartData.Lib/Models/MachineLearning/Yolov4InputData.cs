using Microsoft.ML.OnnxRuntime.Tensors;

namespace SmartData.Lib.Models.MachineLearning
{
    public class Yolov4InputData
    {
        public DenseTensor<float> Input { get; set; }
    }
}
