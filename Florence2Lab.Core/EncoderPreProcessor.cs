using System.Diagnostics;

using Microsoft.ML.OnnxRuntime.Tensors;

namespace FlorenceTwoLab.Core;

public class EncoderPreProcessor
{
    /// <summary>
    /// Processes the provided vision and text features along with their associated tokens,
    /// returning a combined tensor and attention mask suitable for model input.
    /// </summary>
    /// <param name="visionFeatures">The tensor containing vision-based feature embeddings.</param>
    /// <param name="textFeatures">The tensor containing text-based feature embeddings.</param>
    /// <param name="tokenized">A collection of token strings associated with the text features.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description><c>Features</c>: A concatenated tensor of vision and text features.</description></item>
    /// <item><description><c>AttentionMask</c>: A concatenated attention mask corresponding to the input features.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Assumes the input tensors are two-dimensional and that concatenation occurs along the feature axis (axis 1).
    /// Validates dimensional alignment between input features and attention masks using debug assertions.
    /// </remarks>
    public (DenseTensor<float> Features, DenseTensor<long> AttentionMask) Process(Tensor<float> visionFeatures, Tensor<float> textFeatures, IReadOnlyCollection<string> tokenized)
    {
        DenseTensor<float> projectedFeatures = ConcatenateTensors(visionFeatures, textFeatures, 1);

        Tensor<long> visionAttentionMask = CreateAttentionMask(Enumerable.Range(0, visionFeatures.Dimensions[1]).ToArray(), _ => 1L);
        Debug.Assert(visionFeatures.Dimensions[1] == visionAttentionMask.Dimensions[1]);

        Tensor<long> textAttentionMask = CreateAttentionMask(tokenized, t => t == BartTokenizer.PadToken ? 0L : 1L);
        Debug.Assert(textFeatures.Dimensions[1] == textAttentionMask.Dimensions[1]);

        DenseTensor<long> projectedAttentionMask = ConcatenateTensors(visionAttentionMask, textAttentionMask, 1);

        return (projectedFeatures, projectedAttentionMask);
    }

    /// <summary>
    /// Creates an attention mask from a collection of input data using the provided evaluation function.
    /// </summary>
    /// <typeparam name="TIn">The type of the input data.</typeparam>
    /// <typeparam name="TOut">The type of the output mask values.</typeparam>
    /// <param name="data">The input data to evaluate.</param>
    /// <param name="maskEvaluator">A function that determines the mask value for each input element.</param>
    /// <returns>A tensor representing the generated attention mask.</returns>
    private static Tensor<TOut> CreateAttentionMask<TIn, TOut>(IReadOnlyCollection<TIn> data, Func<TIn, TOut> maskEvaluator)
    {
        TOut[] maskData = data.Select(maskEvaluator).ToArray();
        return new DenseTensor<TOut>(maskData, [1, data.Count]);
    }

    /// <summary>
    /// Concatenates two tensors along the specified axis.
    /// </summary>
    /// <typeparam name="T">The type of the tensor elements.</typeparam>
    /// <param name="tensor1">The first tensor to concatenate.</param>
    /// <param name="tensor2">The second tensor to concatenate.</param>
    /// <param name="axis">The axis along which to concatenate. Only axis 1 is supported.</param>
    /// <returns>A new dense tensor containing the concatenated values.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when tensors have different ranks, an invalid axis is provided, or an unsupported axis is used.
    /// </exception>
    private static DenseTensor<T> ConcatenateTensors<T>(Tensor<T> tensor1, Tensor<T> tensor2, int axis)
    {
        if (tensor1.Rank != tensor2.Rank)
        {
            throw new ArgumentException("Tensors must have the same number of dimensions");
        }

        if (axis < 0 || axis >= tensor1.Rank)
        {
            throw new ArgumentException("Invalid axis");
        }

        if (axis != 1)
        {
            throw new ArgumentException("Only concatenation along axis 1 is supported");
        }

        int[] newDimensions = tensor1.Dimensions.ToArray();
        newDimensions[axis] += tensor2.Dimensions[axis];

        DenseTensor<T> result = new DenseTensor<T>(newDimensions);

        // Copy data from tensor1
        for (int i = 0; i < tensor1.Length; i++)
        {
            result.SetValue(i, tensor1.GetValue(i));
        }

        // Copy data from tensor2
        int offset = (int)tensor1.Length;
        for (int i = 0; i < tensor2.Length; i++)
        {
            result.SetValue(offset + i, tensor2.GetValue(i));
        }

        return result;
    }
}
