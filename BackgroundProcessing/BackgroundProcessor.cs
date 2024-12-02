using Microsoft.Extensions.Options;

namespace BackgroundProcessing;

public sealed class BackgroundProcessor : IDisposable
{
    private readonly ILogger<BackgroundProcessor> _logger;
    private readonly SemaphoreSlim _semaphoreSlim;

    public BackgroundProcessor(ILogger<BackgroundProcessor> logger, IOptions<BackgroundProcessingCfg> options)
    {
        _logger = logger;
        _semaphoreSlim = new SemaphoreSlim(options.Value.InitialParallelism, options.Value.MaxParallelism);
    }
    
    public async Task Proceed(Func<Task> task, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            await task.Invoke();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    public async Task Proceed(Action task, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            task.Invoke();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public void Dispose() => _semaphoreSlim.Dispose();
}