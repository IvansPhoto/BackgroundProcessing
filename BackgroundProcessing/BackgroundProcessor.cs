namespace BackgroundProcessing;

public class BackgroundProcessor
{
    private readonly ILogger<BackgroundProcessor> _logger;
    private readonly SemaphoreSlim _semaphoreSlim;

    public BackgroundProcessor(ILogger<BackgroundProcessor> logger)
    {
        _logger = logger;
        _semaphoreSlim = new SemaphoreSlim(4, 4);
    }
    
    public async Task Proceed(Task task, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            await task;
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

    public async Task Proceed<T>(Task<T> task, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            await task;
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
}