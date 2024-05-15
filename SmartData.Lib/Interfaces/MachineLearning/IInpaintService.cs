using System;
using System.Collections.Generic;
using System.Text;

namespace SmartData.Lib.Interfaces.MachineLearning
{
    public interface IInpaintService
    {
        public Task InpaintImageAsync(string inputImagePath, string inputMaskPath, string outputImagePath);
        public Task InpaintImageTilesAsync(string inputImagePath, string inputMaskPath, string outputImagePath);
    }
}
