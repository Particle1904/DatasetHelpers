using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FlorenceTwoLab.Core;

internal sealed class ModelRunner : IDisposable
{
    private readonly string _visionEncoderModelName = "florence2VisionEncoder.onnx";
    private readonly string _embedTokensModelName = "florence2EmbedTokens.onnx";
    private readonly string _encoderModelName = "florence2Encoder.onnx";
    private readonly string _decoderModelName = "florence2Decoder.onnx";

    private readonly string _modelDirectory;

    private readonly InferenceSession _visionEncoder;
    private readonly InferenceSession _embedTokens;
    private readonly InferenceSession _encoder;
    private readonly InferenceSession _decoder;

    private readonly SessionOptions _sessionOptions;
    private bool _useGPU = false;

    public ModelRunner(IOnnxModelPathProvider pathProvider)
    {
        _modelDirectory = pathProvider.OnnxModelDirectory;
        _sessionOptions = CreateSessionOptions(_useGPU);

        // Initialize sessions with model path
        _visionEncoder = new InferenceSession(Path.Combine(_modelDirectory, _visionEncoderModelName), _sessionOptions);
        _embedTokens = new InferenceSession(Path.Combine(_modelDirectory, _embedTokensModelName), _sessionOptions);
        _encoder = new InferenceSession(Path.Combine(_modelDirectory, _encoderModelName), _sessionOptions);
        _decoder = new InferenceSession(Path.Combine(_modelDirectory, _decoderModelName), _sessionOptions);
    }

    /// <summary>
    /// Runs the vision encoder model on the given image input tensor.
    /// </summary>
    /// <param name="imageInput">A float tensor representing pixel values.</param>
    /// <returns>A float tensor of extracted image features.</returns>
    public Tensor<float> RunVisionEncoder(DenseTensor<float> imageInput)
    {
        List<NamedOnnxValue> visionInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("pixel_values", imageInput)
        };

        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> visionOutput = _visionEncoder.Run(visionInputs))
        {
            Tensor<float> imageFeaturesTensor = visionOutput.First(output => output.Name == "image_features").AsTensor<float>();
            return new DenseTensor<float>(imageFeaturesTensor.ToArray(), imageFeaturesTensor.Dimensions.ToArray());
        }
    }

    /// <summary>
    /// Runs the token embedding model on the given input token IDs.
    /// </summary>
    /// <param name="tokens">A tensor of token IDs.</param>
    /// <returns>A float tensor representing embedded tokens.</returns>
    public Tensor<float> EmbedTokens(Tensor<long> tokens)
    {
        List<NamedOnnxValue> embedInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", tokens)
        };

        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> embedOutput = _embedTokens.Run(embedInputs))
        {
            Tensor<float> textFeaturesTensor = embedOutput.First(output => output.Name == "inputs_embeds").AsTensor<float>();
            return new DenseTensor<float>(textFeaturesTensor.ToArray(), textFeaturesTensor.Dimensions.ToArray());
        }
    }

    /// <summary>
    /// Runs the encoder model with the given embeddings and attention mask.
    /// </summary>
    /// <param name="embeddings">The input embeddings tensor.</param>
    /// <param name="attentionMask">The attention mask tensor.</param>
    /// <returns>A tensor representing encoder hidden states.</returns>
    public Tensor<float> RunEncoder(Tensor<float> embeddings, Tensor<long> attentionMask)
    {
        List<NamedOnnxValue> encoderInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("inputs_embeds", embeddings),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask                )
        };

        using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> encoderOutput = _encoder.Run(encoderInputs))
        {
            Tensor<float> encoderHiddenStates = encoderOutput.First(output => output.Name == "last_hidden_state").AsTensor<float>();
            return new DenseTensor<float>(encoderHiddenStates.ToArray(), encoderHiddenStates.Dimensions.ToArray());
        }
    }

    /// <summary>
    /// Runs the autoregressive decoder loop to generate output tokens.
    /// </summary>
    /// <param name="encoderHiddenStates">The hidden states from the encoder.</param>
    /// <param name="encoderAttentionMask">The attention mask used during encoding.</param>
    /// <param name="maxLength">The maximum length of the generated sequence.</param>
    /// <returns>A read-only collection of generated token IDs.</returns>
    public IReadOnlyCollection<long> RunDecoder(Tensor<float> encoderHiddenStates, Tensor<long> encoderAttentionMask, int maxLength = 1024)
    {
        int batchSize = (int)encoderHiddenStates.Dimensions[0];
        if (batchSize > 1) throw new NotSupportedException("This example is simplified for batch size 1.");

        const int decoderStartTokenId = 2; // Should get from tokenizer
        const int eosTokenId = 2; // Should get from tokenizer

        List<long> generatedTokens = new List<long>();
        // This will hold the cache state between loop iterations. We will manage its lifecycle manually.
        NamedOnnxValue[]? pastKeyValues = null;

        // Start with only the decoder_start_token.
        DenseTensor<long> decoderInputIds = new DenseTensor<long>(new long[] { decoderStartTokenId }, new[] { batchSize, 1 });

        string[] decoderOutputNames = new[] {
            "logits", "present.0.decoder.key", "present.0.decoder.value", "present.0.encoder.key", "present.0.encoder.value",
            "present.1.decoder.key", "present.1.decoder.value", "present.1.encoder.key", "present.1.encoder.value",
            "present.2.decoder.key", "present.2.decoder.value", "present.2.encoder.key", "present.2.encoder.value",
            "present.3.decoder.key", "present.3.decoder.value", "present.3.encoder.key", "present.3.encoder.value",
            "present.4.decoder.key", "present.4.decoder.value", "present.4.encoder.key", "present.4.encoder.value",
            "present.5.decoder.key", "present.5.decoder.value", "present.5.encoder.key", "present.5.encoder.value"
        };

        for (int i = 0; i < maxLength; i++)
        {
            Tensor<float> decoderEmbeddings;
            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> embedOutput = EmbedTokensInternal(decoderInputIds))
            {
                // Clone the embedding tensor because the source will be disposed.
                decoderEmbeddings = embedOutput.First().AsTensor<float>().Clone();
            }

            List<NamedOnnxValue> decoderFeeds = new List<NamedOnnxValue>();
            bool useCache = pastKeyValues != null;

            decoderFeeds.Add(NamedOnnxValue.CreateFromTensor("inputs_embeds", decoderEmbeddings));
            decoderFeeds.Add(NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHiddenStates));
            decoderFeeds.Add(NamedOnnxValue.CreateFromTensor("encoder_attention_mask", encoderAttentionMask));
            decoderFeeds.Add(NamedOnnxValue.CreateFromTensor("use_cache_branch", new DenseTensor<bool>(new[] { useCache }, new[] { 1 })));

            // Initialize or add the existing cache.
            if (!useCache)
            {
                pastKeyValues = InitPastKeyValues(batchSize);
            }
            decoderFeeds.AddRange(pastKeyValues);

            NamedOnnxValue[] nextPastKeyValues;
            long nextToken;

            // This using block ensures the raw output from Run() is disposed.
            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _decoder.Run(decoderFeeds, decoderOutputNames))
            {
                Tensor<float> logits = outputs.First(o => o.Name == "logits").AsTensor<float>();
                nextToken = GetNextToken(logits);

                // This method now handles creating the *new* cache state from the outputs.
                nextPastKeyValues = UpdatePastKeyValues(outputs, useCache, pastKeyValues);
            }

            // *** THE CRITICAL FIX FOR MEMORY STABILITY ***
            // Manually dispose the tensors from the PREVIOUS iteration's cache.
            // This immediately frees the large unmanaged memory blocks.
            foreach (NamedOnnxValue kv in pastKeyValues)
            {
                (kv.Value as IDisposable)?.Dispose();
            }

            // Point to the new cache for the next iteration.
            pastKeyValues = nextPastKeyValues;

            generatedTokens.Add(nextToken);

            if (nextToken == eosTokenId)
            {
                break;
            }

            // Prepare the input for the next loop.
            decoderInputIds = new DenseTensor<long>(new[] { nextToken }, new[] { batchSize, 1 });
        }

        // Final cleanup of the last cache state
        if (pastKeyValues != null)
        {
            foreach (NamedOnnxValue kv in pastKeyValues)
            {
                (kv.Value as IDisposable)?.Dispose();
            }
        }

        return generatedTokens;
    }

    /// <summary>
    /// Runs the token embedding model internally and returns disposable output.
    /// </summary>
    /// <param name="tokens">The input tokens tensor.</param>
    /// <returns>A disposable collection of ONNX output values.</returns>
    private IDisposableReadOnlyCollection<DisposableNamedOnnxValue> EmbedTokensInternal(Tensor<long> tokens)
    {
        return _embedTokens.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_ids", tokens) });
    }

    /// <summary>
    /// Initializes empty past key-value tensors for the decoder cache.
    /// </summary>
    /// <param name="batchSize">The batch size (must match input data).</param>
    /// <returns>An array of named ONNX values representing the decoder cache.</returns>
    private NamedOnnxValue[] InitPastKeyValues(int batchSize)
    {
        const int numDecoderLayers = 6;
        const int numDecoderHeads = 12;
        const int decoderDimKv = 64;

        List<NamedOnnxValue> cache = new List<NamedOnnxValue>();

        // CRITICAL FIX: The sequence length dimension for ALL past_key_values
        // must be initialized to 0 to signal an empty cache.
        // The model will then correctly populate the encoder cache from encoder_hidden_states
        // on the first run.
        int[] initialEncoderDims = new[] { batchSize, numDecoderHeads, 0, decoderDimKv };
        int[] initialDecoderDims = new[] { batchSize, numDecoderHeads, 0, decoderDimKv };

        for (int i = 0; i < numDecoderLayers; ++i)
        {
            cache.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.encoder.key", new DenseTensor<float>(initialEncoderDims)));
            cache.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.encoder.value", new DenseTensor<float>(initialEncoderDims)));
            cache.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.decoder.key", new DenseTensor<float>(initialDecoderDims)));
            cache.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.decoder.value", new DenseTensor<float>(initialDecoderDims)));
        }
        return cache.ToArray();
    }

    /// <summary>
    /// Updates the past key-value cache for the decoder using the latest output.
    /// </summary>
    /// <param name="decoderOutputs">The outputs from the decoder model.</param>
    /// <param name="useCache">Indicates whether caching is enabled.</param>
    /// <param name="oldPastKeyValues">The previous cache values.</param>
    /// <returns>An updated array of named ONNX values representing the new cache.</returns>
    private NamedOnnxValue[] UpdatePastKeyValues(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> decoderOutputs, bool useCache,
        NamedOnnxValue[] oldPastKeyValues)
    {
        List<NamedOnnxValue> newPastKeyValues = new List<NamedOnnxValue>();

        foreach (NamedOnnxValue oldKv in oldPastKeyValues)
        {
            string presentName = oldKv.Name.Replace("past_key_values", "present");

            // If we are using the cache AND it's an encoder key/value, we can reuse the old one.
            // But we must CLONE it because the old one will be disposed by the calling loop.
            if (useCache && presentName.Contains("encoder"))
            {
                newPastKeyValues.Add(NamedOnnxValue.CreateFromTensor(oldKv.Name, (oldKv.Value as Tensor<float>).Clone()));
            }
            else
            {
                // Otherwise, we take the new value from the model output and clone it.
                DisposableNamedOnnxValue presentValue = decoderOutputs.First(o => o.Name == presentName);
                newPastKeyValues.Add(NamedOnnxValue.CreateFromTensor(oldKv.Name, (presentValue.Value as Tensor<float>).Clone()));
            }
        }
        return newPastKeyValues.ToArray();
    }

    /// <summary>
    /// Selects the next token to decode based on the output logits.
    /// </summary>
    /// <param name="logits">The logits tensor from the decoder output.</param>
    /// <returns>The token ID with the highest score.</returns>
    private long GetNextToken(Tensor<float> logits)
    {
        // The logits tensor in the generation loop has a shape of [batch_size, sequence_length, vocab_size].
        // For our case, batch_size is 1 and sequence_length is always 1.
        // So the shape is [1, 1, vocab_size].
        int vocabSize = logits.Dimensions[2];

        float maxProb = float.MinValue;
        long maxToken = 0;

        // We iterate through the last dimension (the vocabulary) to find the highest logit value.
        // The indexer logits[batch, sequence, vocab_id] is the correct way to access values
        // from the abstract Tensor<T> class.
        for (int i = 0; i < vocabSize; i++)
        {
            float prob = logits[0, 0, i]; // Accessing the value at the specific index
            if (prob > maxProb)
            {
                maxProb = prob;
                maxToken = (long)i;
            }
        }

        return maxToken;
    }

    /// <summary>
    /// Creates ONNX session options with CPU or GPU execution providers.
    /// </summary>
    /// <param name="useGPU">True to attempt using GPU execution providers.</param>
    /// <returns>A configured <see cref="SessionOptions"/> instance.</returns>
    private SessionOptions CreateSessionOptions(bool useGPU)
    {
        // Create separate inference sessions for each model component
        int[] gpuIdsToTry = { 0, 1 };

        SessionOptions sessionOptions = new SessionOptions();
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        sessionOptions.IntraOpNumThreads = 1;
        sessionOptions.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
        sessionOptions.EnableMemoryPattern = false;
        sessionOptions.EnableCpuMemArena = true;

        if (_useGPU)
        {
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

        return sessionOptions;
    }

    /// <summary>
    /// Disposes all ONNX inference sessions and associated resources.
    /// </summary>
    public void Dispose()
    {
        _visionEncoder?.Dispose();
        _encoder?.Dispose();
        _embedTokens?.Dispose();
        _decoder?.Dispose();
        _sessionOptions?.Dispose();
    }
}