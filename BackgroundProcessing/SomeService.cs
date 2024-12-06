namespace BackgroundProcessing;

public sealed class SomeService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SomeService> _logger;
    private readonly HttpClient _httpClient;

    public SomeService(ILogger<SomeService> logger, IServiceProvider serviceProvider, HttpClient httpClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
    }

    public void DoAsyncBackground(string text, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = _serviceProvider.GetRequiredService<BackgroundProcessor>();

        processor.Proceed(() => AsyncJob(text, cancellationToken), cancellationToken);
        _logger.LogInformation("Scheduled Async work with {Text}", text);
    }

    public void DoSyncBackground(string text, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = _serviceProvider.GetRequiredService<BackgroundProcessor>();
        
        processor.Proceed(() => SyncJob(text, cancellationToken), cancellationToken);
        _logger.LogInformation("Scheduled Sync work with {Text}", text);
    }

    public async Task DoAsyncBackgroundAwait(string text, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = _serviceProvider.GetRequiredService<BackgroundProcessorQueue>();

        _logger.LogInformation("Sending Async work with {Text} to queue", text);
        await processor.AwaitProceed(() => AsyncJob(text, cancellationToken), cancellationToken);
        _logger.LogInformation("Sent Async work with {Text} to queue", text);
    }
    
    public async Task DoSyncBackgroundAwait(string text, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = _serviceProvider.GetRequiredService<BackgroundProcessorQueue>();
        
        _logger.LogWarning("Sending Sync work with {Text} to queue", text);
        await processor.AwaitProceed(() => SyncJob(text, cancellationToken), cancellationToken);
        _logger.LogWarning("Sent Sync work with {Text} to queue", text);
    }
    
    private async Task AsyncJob(string text, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Work started with {Text}", text);
        var response = await _httpClient.GetAsync("/get", cancellationToken);
        _logger.LogInformation("Work stopped with {Text} {StatusCode}", text, response.StatusCode);
    }

    private void SyncJob(string text, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Work started with {Text}", text);
        float number = 0;
        for (var i = 1; i < 100_000_000; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            number += i * 3;
            number /= i / (float)1.15;
        }

        _logger.LogInformation("Work finished with {Text} {Number}", text, number.ToString("F"));
    }
}