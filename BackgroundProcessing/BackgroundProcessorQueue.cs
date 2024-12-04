using Microsoft.Extensions.Options;

namespace BackgroundProcessing;

public sealed class BackgroundProcessorQueue : IDisposable
{
    private readonly ILogger<BackgroundProcessorQueue> _logger;
    private readonly SemaphoreSlim _semaphoreSlimQueue;

    public BackgroundProcessorQueue(ILogger<BackgroundProcessorQueue> logger, IOptions<BackgroundProcessingCfg> options)
    {
        _logger = logger;
        _semaphoreSlimQueue = new SemaphoreSlim(options.Value.ConcurrentQueue, options.Value.ConcurrentQueue);
    }

    public async Task AwaitProceed(Func<Task> task, CancellationToken cancellationToken)
    {
        await _semaphoreSlimQueue.WaitAsync(cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                await task.Invoke();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "_");
            }
            finally
            {
                _semaphoreSlimQueue.Release();
            }
        }, cancellationToken);
    }
    
    public async Task AwaitProceed(Action task, CancellationToken cancellationToken)
    {
        await _semaphoreSlimQueue.WaitAsync(cancellationToken);

        _ = Task.Run(() =>
        {
            try
            {
                task.Invoke();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "_");
            }
            finally
            {
                _semaphoreSlimQueue.Release();
            }
        }, cancellationToken);
    }

    public int AvailableSpace() => _semaphoreSlimQueue.CurrentCount;
    
    public void Dispose() => _semaphoreSlimQueue.Dispose();
}