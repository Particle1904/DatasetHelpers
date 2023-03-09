namespace DatasetHelpers.Models
{
    public class ResizeParams
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string FilePath { get; set; }
        public CountdownEvent CountdownEvent { get; set; }
    }
}
