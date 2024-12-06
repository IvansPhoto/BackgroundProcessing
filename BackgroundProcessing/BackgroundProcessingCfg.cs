namespace BackgroundProcessing;

public sealed class BackgroundProcessingCfg
{
    public const string Section = "BackgroundProcessingCfg";
    public int Parallelism { get; set; } = 3;
    public int ConcurrentQueue { get; set; } = 10;
}