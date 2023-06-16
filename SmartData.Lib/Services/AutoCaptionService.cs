using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

namespace SmartData.Lib.Services
{
    public class AutoCaptionService : BaseAIConsumer<BLIPInputData, BLIPOutputData>, IAutoCaptionService
    {
        public AutoCaptionService(IImageProcessorService imageProcessorService, ITagProcessorService tagProcessorService, string modelPath) : base(imageProcessorService, tagProcessorService, modelPath)
        {
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "input_ids", "pixel_values" };
        }

        protected override string[] GetOutputColumns()
        {
            return new string[] { "output" };
        }
    }
}