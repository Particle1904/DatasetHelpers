using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FlorenceTwoLab.Core;

internal sealed class ModelRunner : IDisposable
{
    private readonly InferenceSession _decoder;
    private readonly InferenceSession _embedTokens;
    private readonly InferenceSession _encoder;
    private readonly InferenceSession _visionEncoder;

    private bool _useGPU = true;

    public ModelRunner(IOnnxModelPathProvider pathProvider)
    {
        string modelDirectory = pathProvider.OnnxModelDirectory;

        // Create separate inference sessions for each model component
        int[] gpuIdsToTry = { 0, 1 };

        SessionOptions sessionOptions = new SessionOptions();

        if (_useGPU)
        {
            sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            sessionOptions.IntraOpNumThreads = 1;
            sessionOptions.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            sessionOptions.EnableMemoryPattern = false;

            try
            {
                sessionOptions.AppendExecutionProvider_DML(0);
            }
            catch (Exception) { /* DML Failed */ }

            try
            {
                sessionOptions.AppendExecutionProvider_CUDA(0);
            }
            catch (Exception) { /* CUDA Failed */ }

            try
            {
                sessionOptions.AppendExecutionProvider_ROCm(0);
            }
            catch (Exception) { /* ROCm Failed */ }
        }

        sessionOptions.ApplyConfiguration();

        // Initialize sessions with model path
        _decoder = new InferenceSession(Path.Combine(modelDirectory, "florence2Decoder.onnx"), sessionOptions);
        _embedTokens = new InferenceSession(Path.Combine(modelDirectory, "florence2EmbedTokens.onnx"), sessionOptions);
        _encoder = new InferenceSession(Path.Combine(modelDirectory, "florence2Encoder.onnx"), sessionOptions);
        _visionEncoder = new InferenceSession(Path.Combine(modelDirectory, "florence2VisionEncoder.onnx"), sessionOptions);
    }

    /// <summary>
    /// Asynchronously runs the vision encoder on the input image tensor to extract image features.
    /// </summary>
    /// <param name="imageInput">
    /// A <see cref="DenseTensor{T}"/> representing the input image tensor with shape [batch_size, 3, height, width].
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a tensor of image features
    /// with shape [batch_size, sequence_length, 768].
    /// </returns>
    public async Task<Tensor<float>> RunVisionEncoderAsync(DenseTensor<float> imageInput)
    {
        // Run vision encoder to get image features
        // Input: pixel_values [batch_size, 3, height, width]
        // Output: image_features [batch_size, sequence_length, 768]
        List<NamedOnnxValue> visionInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("pixel_values", imageInput)
        };

        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> visionOutput = await RunInferenceAsync(_visionEncoder, visionInputs))
        {
            DisposableNamedOnnxValue imageFeaturesTensor = visionOutput.First(o => o.Name == "image_features");
            return imageFeaturesTensor.Value as Tensor<float> ?? throw new InvalidCastException("image_features tensor is not of type Tensor<float>");
        }
    }

    /// <summary>
    /// Asynchronously embeds input token IDs into feature vectors using the token embedding model.
    /// </summary>
    /// <param name="tokens">
    /// A tensor of token IDs with shape [batch_size, sequence_length].
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a tensor of embedded tokens
    /// with shape [batch_size, sequence_length, 768].
    /// </returns>
    public async Task<Tensor<float>> EmbedTokensAsync(Tensor<long> tokens)
    {
        // Run token embedding model to get text features
        // Input: input_ids [batch_size, sequence_length]
        // Output: inputs_embeds [batch_size, sequence_length, 768]
        List<NamedOnnxValue> embedInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", tokens)
        };

        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> embedOutput = await RunInferenceAsync(_embedTokens, embedInputs))
        {
            Tensor<float> textFeaturesTensor = embedOutput.First(o => o.Name == "inputs_embeds").AsTensor<float>();
            return textFeaturesTensor;
        }
    }

    /// <summary>
    /// Asynchronously runs the encoder on embedded tokens with an attention mask to obtain hidden states.
    /// </summary>
    /// <param name="embeddings">
    /// A tensor of embedded tokens with shape [batch_size, encoder_sequence_length, 768].
    /// </param>
    /// <param name="attentionMask">
    /// A tensor representing the attention mask with shape [batch_size, encoder_sequence_length].
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a tensor of encoder hidden states
    /// with shape [batch_size, encoder_sequence_length, 768].
    /// </returns>
    public async Task<Tensor<float>> RunEncoderAsync(Tensor<float> embeddings, Tensor<long> attentionMask)
    {
        // Step 2: Run encoder on image features
        // Inputs: 
        // - inputs_embeds [batch_size, encoder_sequence_length, 768]
        // - attention_mask [batch_size, encoder_sequence_length]
        List<NamedOnnxValue> encoderInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("inputs_embeds", embeddings),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask                )
        };

        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> encoderOutput = await RunInferenceAsync(_encoder, encoderInputs))
        {
            Tensor<float> encoderHiddenStates = encoderOutput.First(o => o.Name == "last_hidden_state").AsTensor<float>();
            return encoderHiddenStates;
        }
    }

    /// <summary>
    /// Asynchronously runs the decoder to generate a sequence of tokens based on encoder hidden states and attention mask.
    /// </summary>
    /// <param name="encoderHiddenStates">
    /// The tensor containing encoder hidden states with shape [batch_size, sequence_length, 768].
    /// </param>
    /// <param name="encoderAttentionMask">
    /// The tensor representing the encoder attention mask with shape [batch_size, sequence_length].
    /// </param>
    /// <param name="maxLength">
    /// The maximum length of the generated token sequence. Defaults to 1024.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a read-only collection of generated token IDs.
    /// </returns>
    public async Task<IReadOnlyCollection<long>> RunDecoderAsync(Tensor<float> encoderHiddenStates, Tensor<long> encoderAttentionMask, int maxLength = 1024)
    {
        // this value comes from the "config.json" of the "onnx-community/Florence-2-*" repo.
        const int decoderStartTokenId = 2; // Initialize with decoder start token (end token?)
        const int eosTokenId = 2; // End of sentence token, TODO: we could get this from the tokenizer

        // Initialize with decoder start token
        List<long> generatedTokens = new List<long> { decoderStartTokenId };

        // dry run???
        {
            // Create decoder inputs from current tokens
            DenseTensor<long> decoderInputIds = new DenseTensor<long>(generatedTokens.ToArray(), [1, generatedTokens.Count]);
            Tensor<float> decoderEmbeddings = await EmbedTokensAsync(decoderInputIds);

            // Run decoder
            NamedOnnxValue[] decoderInputs =
            [
                NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHiddenStates),
                NamedOnnxValue.CreateFromTensor("encoder_attention_mask", encoderAttentionMask),
                NamedOnnxValue.CreateFromTensor("inputs_embeds", decoderEmbeddings)
            ];

            _ = await RunInferenceAsync(_decoder, decoderInputs);
            // var logits = outputs.First(o => o.Name == "logits").AsTensor<float>();
        }

        for (int i = 0; i < maxLength; i++)
        {
            // Create decoder inputs from current tokens
            DenseTensor<long> decoderInputIds = new DenseTensor<long>(generatedTokens.ToArray(), [1, generatedTokens.Count]);
            Tensor<float> decoderEmbeddings = await EmbedTokensAsync(decoderInputIds);

            // Run decoder
            NamedOnnxValue[] decoderInputs =
            [
                NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHiddenStates),
                NamedOnnxValue.CreateFromTensor("encoder_attention_mask", encoderAttentionMask),
                NamedOnnxValue.CreateFromTensor("inputs_embeds", decoderEmbeddings)
            ];

            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = await RunInferenceAsync(_decoder, decoderInputs);
            Tensor<float> logits = outputs.First(o => o.Name == "logits").AsTensor<float>();

            // Get next token (greedy selection from last position)
            long nextToken = GetNextToken(logits);

            // Stop if we hit EOS token
            if (nextToken == eosTokenId)
            {
                break;
            }

            generatedTokens.Add(nextToken);

            //if (HasRepeatingPattern(generatedTokens, 2, 10))
            //{
            //    Console.WriteLine("[Decoder] Repeating token pattern detected. Stopping generation.");
            //    break;
            //}
        }

        return generatedTokens;
    }

    private static bool HasRepeatingPattern(List<long> tokens, int minPatternSize, int maxPatternSize)
    {
        int len = tokens.Count;

        for (int size = minPatternSize; size <= maxPatternSize; size++)
        {
            if (len < size * 2) continue;

            bool match = true;
            for (int i = 0; i < size; i++)
            {
                if (tokens[len - 1 - i] != tokens[len - 1 - size - i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Selects the token with the highest logit probability from the decoder output logits tensor.
    /// </summary>
    /// <param name="logits">
    /// The logits tensor with shape [batch_size, sequence_length, vocab_size].
    /// </param>
    /// <returns>The token ID with the highest probability at the last position in the sequence.</returns>
    private static long GetNextToken(Tensor<float> logits)
    {
        // Get last position logits
        int lastLogits = logits.Dimensions[1] - 1;
        int vocabSize = logits.Dimensions[2];

        // Find max probability token
        float maxProb = float.MinValue;
        long maxToken = 0L;

        for (int i = 0; i < vocabSize; i++)
        {
            float prob = logits[0, lastLogits, i];
            if (prob > maxProb)
            {
                maxProb = prob;
                maxToken = i;
            }
        }

        return maxToken;
    }

    /// <summary>
    /// Asynchronously runs inference on the provided <see cref="InferenceSession"/> with the specified inputs.
    /// </summary>
    /// <param name="session">The ONNX inference session to run.</param>
    /// <param name="inputs">The named inputs to pass to the inference session.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a collection of named ONNX values returned from the session.
    /// </returns>
    private static async Task<IDisposableReadOnlyCollection<DisposableNamedOnnxValue>> RunInferenceAsync(InferenceSession session, IReadOnlyCollection<NamedOnnxValue> inputs)
    {
        return await Task.Run(() => session.Run(inputs));
    }

    public void Dispose()
    {
        _decoder.Dispose();
        _embedTokens.Dispose();
        _encoder.Dispose();
        _visionEncoder.Dispose();
    }
}
