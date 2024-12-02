namespace BackgroundProcessing;

public class BackgroundProcessingCfg
{
    public const string Section = "BackgroundProcessingCfg";
    public int MinParallelism { get; set; } = 1;
    public int MaxParallelism { get; set; } = 10;
}