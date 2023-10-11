namespace DatasetProcessor.src.Enums
{
    public enum ProcessingStatus : byte
    {
        Idle,
        Running,
        Finished,
        BackingUp,
        LoadingModel
    }
}
