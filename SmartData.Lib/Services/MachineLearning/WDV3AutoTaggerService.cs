using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services.MachineLearning
{
    public class WDV3AutoTaggerService : WDAutoTaggerService
    {
        // This model inputs and outputs maps 1:1 to WD 1.4 architecture, we just need to use different files.
        // This class is here specifically for the sake of organization and brevity;
        // making it easier to manage the 2 services when calling it
        public WDV3AutoTaggerService(IImageProcessorService imageProcessorService, ITagProcessorService tagProcessorService,
            string modelPath, string tagsPath) :
            base(imageProcessorService, tagProcessorService, modelPath, tagsPath)
        {
        }
    }
}
