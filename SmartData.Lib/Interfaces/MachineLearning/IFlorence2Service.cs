using FlorenceTwoLab.Core;

using SixLabors.ImageSharp;

using SmartData.Lib.Enums;

namespace Interfaces.MachineLearning
{
    public interface IFlorence2Service
    {
        public Task CaptionImagesAsync(string inputFolderPath, string outputFolderPath, Florence2CaptionTask captionTask);
        public Task<Florence2Result> ProcessAsync(Image image, Florence2Query query);
    }
}
