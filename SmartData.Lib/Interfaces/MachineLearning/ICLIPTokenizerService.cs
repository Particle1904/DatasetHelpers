namespace SmartData.Lib.Interfaces.MachineLearning
{
    public interface ICLIPTokenizerService
    {
        public Task<int> CountTokensAsync(string inputText);
    }
}
