namespace BackgroundProcessing;

public sealed class BackgroundProcessingCfg
{
    public const string Section = "BackgroundProcessingCfg";
    public int InitialParallelism { get; set; } = 3;
    public int MaxParallelism { get; set; } = 3;
}