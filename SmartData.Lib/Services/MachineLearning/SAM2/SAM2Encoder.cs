using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.MachineLearning.SAM2;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning.SAM2
{
    class SAM2Encoder : BaseAIConsumer<SAM2EncoderInputData, SAM2EncoderOutputData>
    {
        private readonly IImageProcessorService _imageProcessor;

        public SAM2Encoder(IImageProcessorService imageProcessor, string modelPath) : base(modelPath)
        {
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "image" };
        }

        protected override string[] GetOutputColumns()
        {
            return new string[] { "high_res_feats_0", "high_res_feats_1", "image_embed" };
        }

        public async Task EncodeImageEmbeds(string inputImagePath)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
            }

            SAM2EncoderInputData inputData = await _imageProcessor.ProcessImageForSAM2EncodingAsync(inputImagePath);
        }
    }
}
