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
        Task.Run(() =>
        {
            _logger.LogInformation("Scheduled async work with {Text}", text);
            return processor.Proceed(() => AsyncJob(text, cancellationToken), cancellationToken);
        }, cancellationToken);
    }
    
    public void DoSyncBackground(string text, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = _serviceProvider.GetRequiredService<BackgroundProcessor>();
        Task.Run(() =>
        {
            _logger.LogInformation("Scheduled sync work with {Text}", text);
            return processor.Proceed(() => SyncJob(text, cancellationToken), cancellationToken);
        }, cancellationToken);
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
        var number = 0;
        for (var i = 1; i < 100_000_000; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            
            number += i;
            number /= i;
        }
        _logger.LogInformation("Work stopped with {Text} {Number}", text, number);
    }
}