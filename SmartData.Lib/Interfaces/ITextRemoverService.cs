namespace SmartData.Lib.Interfaces
{
    public interface ITextRemoverService
    {
        public Task RemoveTextFromImagesAsync(string inputFolderPath, string outputFolderPath);
    }
}
