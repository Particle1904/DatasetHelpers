using SmartData.Lib.Models.MachineLearning.SAM2;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning.SAM2
{
    class SAM2Decoder : BaseAIConsumer<SAM2DecoderInputData, SAM2DecoderOutputData>
    {
        public SAM2Decoder(string modelPath) : base(modelPath)
        {
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "image_embed", "high_res_feats_0", "high_res_feats_1", "point_coords",
                "point_labels", "mask_input", "has_mask_input", "orig_im_size" };
        }

        protected override string[] GetOutputColumns()
        {
            return new string[] { "masks", "iou_predictions" };
        }
    }
}
