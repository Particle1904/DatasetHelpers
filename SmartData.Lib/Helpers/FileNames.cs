namespace SmartData.Lib.Helpers
{
    public static class FileNames
    {
        #region Tag Generators
        public const string WDOnnxFileName = "wdModel.onnx";
        public const string WDCsvFileName = "wdTags.csv";

        public const string WDV3OnnxFileName = "wdV3Model.onnx";
        public const string WDV3CsvFileName = "wdV3Tags.csv";

        public const string E621OnnxFileName = "e621Model.onnx";
        public const string E621CsvFileName = "e621Tags.csv";

        public const string JoyTagOnnxFileName = "jtModel.onnx";
        public const string JoyTagCsvFileName = "jtTags.csv";
        #endregion

        #region Yolo Models
        public const string YoloV4OnnxFileName = "yolov4.onnx";
        #endregion region

        #region Others/Onnx Extensions
        public const string CLIPTokenixerOnnxFileName = "cliptokenizer.onnx";
        #endregion

        #region Upscalers
        public const string SwinIRFileName = "swinIR.onnx";
        public const string Swin2SRFileName = "swin2SR.onnx";
        #endregion
    }
}
