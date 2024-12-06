using Microsoft.Extensions.Options;

namespace BackgroundProcessing;

public sealed class BackgroundProcessor : IDisposable
{
    private readonly ILogger<BackgroundProcessor> _logger;
    private readonly SemaphoreSlim _semaphoreSlim;

    public BackgroundProcessor(ILogger<BackgroundProcessor> logger, IOptions<BackgroundProcessingCfg> options)
    {
        _logger = logger;
        _semaphoreSlim = new SemaphoreSlim(options.Value.Parallelism, options.Value.Parallelism);
    }

    public void Proceed(Func<Task> task, CancellationToken cancellationToken) =>
        Task.Run(async () =>
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
        }, cancellationToken);

    public void Proceed(Action action, CancellationToken cancellationToken) =>
        Task.Run(async () =>
        {
            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken);
                action.Invoke();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }, cancellationToken);

    public void Dispose() => _semaphoreSlim.Dispose();
}