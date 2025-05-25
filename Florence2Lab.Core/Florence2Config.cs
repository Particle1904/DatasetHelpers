namespace FlorenceTwoLab.Core;

public interface IOnnxModelPathProvider
{
    string OnnxModelDirectory { get; }
}

public interface IMetadataPathProvider
{
    string MetadataDirectory { get; }
}

public sealed class Florence2Config : IOnnxModelPathProvider, IMetadataPathProvider
{
    public string OnnxModelDirectory { get; init; } = "Models";
    public string MetadataDirectory { get; init; } = "Utils";

}