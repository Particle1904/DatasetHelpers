using FlorenceTwoLab.Core;

using SixLabors.ImageSharp;

namespace Interfaces.MachineLearning
{
    public interface IFlorence2Service
    {
        public Task<Florence2Result> ProcessAsync(Image image, Florence2Query query);
    }
}
