using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;

namespace FlorenceTwoLab.Core;

public partial class Florence2Pipeline : IDisposable
{
    private readonly ImageProcessor _imageProcessor;
    private readonly BartTokenizer _tokenizer;
    private readonly ModelRunner _modelRunner;
    private readonly EncoderPreProcessor _encoderPreprocessor;
    private readonly DecoderPostProcessor _postProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="Florence2Pipeline"/> class with the specified components.
    /// </summary>
    /// <param name="imageProcessor">The image processor responsible for preparing image input.</param>
    /// <param name="tokenizer">The tokenizer used for converting text prompts into token IDs.</param>
    /// <param name="modelRunner">The model runner that executes vision, encoder, and decoder models.</param>
    /// <param name="encoderPreProcessor">The preprocessor that combines vision and text features for encoding.</param>
    /// <param name="postProcessor">The post-processor that converts model output into final structured results.</param>
    private Florence2Pipeline(ImageProcessor imageProcessor, BartTokenizer tokenizer, ModelRunner modelRunner,
        EncoderPreProcessor encoderPreProcessor, DecoderPostProcessor postProcessor)
    {
        _imageProcessor = imageProcessor;
        _tokenizer = tokenizer;
        _modelRunner = modelRunner;
        _encoderPreprocessor = encoderPreProcessor;
        _postProcessor = postProcessor;
    }

    /// <summary>
    /// Asynchronously creates and configures a new instance of the <see cref="Florence2Pipeline"/> class using the specified configuration.
    /// </summary>
    /// <param name="config">The configuration object containing paths and metadata for model setup.</param>
    /// <returns>A task representing the asynchronous operation. The result is an initialized <see cref="Florence2Pipeline"/> instance.</returns>
    public static async Task<Florence2Pipeline> CreateAsync(Florence2Config config)
    {
        ImageProcessor imageProcessor = new ImageProcessor();
        BartTokenizer tokenizer = await BartTokenizer.FromPretrainedAsync(config.MetadataDirectory);
        ModelRunner modelRunner = new ModelRunner(config);
        EncoderPreProcessor encoderPreProcessor = new EncoderPreProcessor();
        DecoderPostProcessor postProcessor = new DecoderPostProcessor();

        return new Florence2Pipeline(imageProcessor, tokenizer, modelRunner, encoderPreProcessor, postProcessor);
    }

    /// <summary>
    /// Asynchronously processes the given image and natural language query to produce structured inference results.
    /// </summary>
    /// <param name="image">The input image to be analyzed.</param>
    /// <param name="query">The query object containing a task type and a text prompt.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a <see cref="Florence2Result"/> containing the processed output.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the provided prompt is null, empty, or consists only of whitespace.</exception>
    /// <remarks>
    /// The pipeline performs image preprocessing, tokenization, multimodal feature fusion, encoder-decoder inference,
    /// and output post-processing. The final result is shaped by the specified task type in the query.
    /// </remarks>
    public Florence2Result Process(Image image, Florence2Query query)
    {
        (Florence2TaskType taskType, string prompt) = query;

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty");
        }

        // 1. Vision
        DenseTensor<float> processedImage = _imageProcessor.ProcessImage(image, false);
        Tensor<float> visionFeatures = _modelRunner.RunVisionEncoder(processedImage);

        // 2. Text
        List<string> tokenized = _tokenizer.Tokenize(prompt);
        //Debug.WriteLine($"Input tokens: '{string.Join("', '", tokenized)}'");

        DenseTensor<long> inputIds = new DenseTensor<long>(_tokenizer.ConvertTokensToIds(tokenized).Select(i => (long)i).ToArray(), [1, tokenized.Count]);
        Tensor<float> textFeatures = _modelRunner.EmbedTokens(inputIds);

        // 3. Concatenate vision and text features
        (DenseTensor<float> projectedFeatures, DenseTensor<long> projectedAttentionMask) = _encoderPreprocessor.Process(visionFeatures, textFeatures, tokenized);

        // 4. Run encoder to get hidden states for decoder
        Tensor<float> encoderHiddenStates = _modelRunner.RunEncoder(projectedFeatures, projectedAttentionMask);

        // 5. Decoder in autoregressive mode to generate output text
        IReadOnlyCollection<long> decoderOutput = _modelRunner.RunDecoder(encoderHiddenStates, projectedAttentionMask);

        string text = _tokenizer.Decode(decoderOutput.Select(f => (int)f).ToList());

        // 6. Post-processing
        return _postProcessor.ProcessAsync(text, taskType, true, image.Width, image.Height).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Disposes of the <see cref="Florence2Pipeline"/> instance, releasing any resources it holds.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Dispose()
    {
        _modelRunner.Dispose();
    }
}
