using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FlorenceTwoLab.Core;

public class ModelOutput : IDisposable
{
    private readonly IDisposableReadOnlyCollection<DisposableNamedOnnxValue> _outputs;

    public ModelOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs)
    {
        _outputs = outputs;
    }

    /// <summary>
    /// Retrieves the logits tensor from the model output.
    /// </summary>
    /// <returns>The logits as a <see cref="Tensor{float}"/>.</returns>
    /// <remarks>
    /// The logits tensor is identified by the name "logits" within the outputs.
    /// </remarks>
    public Tensor<float> GetLogits()
    {
        return _outputs.First(o => o.Name == "logits").AsTensor<float>();
    }

    /// <summary>
    /// Retrieves all present tensors from the model output.
    /// </summary>
    /// <returns>
    /// A read-only list of tensors representing the present states, where each tensor's name starts with "present.".
    /// </returns>
    public IReadOnlyList<Tensor<float>> GetPresent()
    {
        List<Tensor<float>> presentTensors = new List<Tensor<float>>();

        foreach (DisposableNamedOnnxValue output in _outputs)
        {
            if (output.Name.StartsWith("present."))
            {
                presentTensors.Add(output.AsTensor<float>());
            }
        }

        return presentTensors;
    }

    public void Dispose()
    {
        _outputs?.Dispose();
    }
}
