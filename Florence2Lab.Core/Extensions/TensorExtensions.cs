using Microsoft.ML.OnnxRuntime.Tensors;

namespace FlorenceTwoLab.Core.Extensions;

public static class TensorExtensions
{
    /// <summary>
    /// Concatenates two tensors along the specified axis.
    /// </summary>
    /// <typeparam name="T">The type of the tensor elements.</typeparam>
    /// <param name="first">The first tensor to concatenate.</param>
    /// <param name="second">The second tensor to concatenate.</param>
    /// <param name="axis">The axis along which to concatenate the tensors. Defaults to 0.</param>
    /// <returns>A new tensor that is the result of concatenating the two input tensors along the specified axis.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>The tensors do not have the same rank (number of dimensions).</description></item>
    ///   <item><description>The specified axis is invalid (less than 0 or greater than or equal to the number of dimensions).</description></item>
    ///   <item><description>The dimensions of the tensors do not match for all axes except the concatenation axis.</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="NotImplementedException">
    /// Thrown when concatenation is attempted along an axis where any preceding dimension is greater than 1.
    /// Only concatenation along axis 0 or when all dimensions before the axis are 1 is currently supported.
    /// </exception>
    /// <remarks>
    /// This method performs a shallow validation and copy operation. Concatenation is only supported under specific dimensional constraints.
    /// </remarks>
    public static Tensor<T> Concatenate<T>(this Tensor<T> first, Tensor<T> second, int axis = 0)
    {
        if (first.Rank != second.Rank)
        {
            throw new ArgumentException("Tensors must have the same rank (number of dimensions).");
        }

        if (axis < 0 || axis >= first.Dimensions.Length)
        {
            throw new ArgumentException("Invalid axis.");
        }

        for (int i = 0; i < first.Dimensions.Length; i++)
        {
            if (i != axis && first.Dimensions[i] != second.Dimensions[i])
            {
                throw new ArgumentException("Tensors must have the same dimensions except for the concatenation axis.");
            }
        }

        int[] newDimensions = new int[first.Dimensions.Length];
        newDimensions[axis] += second.Dimensions[axis];

        DenseTensor<T> result = new DenseTensor<T>(newDimensions);

        // Can we use flat copy?
        if (axis == 0 || newDimensions.Take(axis).All(d => d == 1))
        {
            int j = 0;
            // Copy data from tensor1
            for (int i = 0; i < first.Length; i++)
            {
                result[j++] = first[i];
            }

            // Copy data from tensor2
            for (int i = 0; i < second.Length; i++)
            {
                result[j++] = second[i];
            }
        }
        else
        {
            throw new NotImplementedException("All dimensions before the concatenation axis must be 1.");
        }

        return result;
    }
}
