# Как ограничить количество одновременно выполняемых задач в фоне
Написание этой статьи навеяно статьёй "[Task изнутри: управление потоками в .NET и создание своих планировщиков](https://habr.com/ru/companies/otus/articles/861074/)". Одна из проблем озвученная в данной статье это ограничение числа одновременно выполняемых задач.
Код проекта доступ на [GitHub](https://github.com/IvansPhoto/BackgroundProcessing).

## Немного подробнее о задаче.
В современном .NET для выполнения задачи в фоне, (запуск операций на потоке отличном от текущего) достаточно вызвать Task.Run(). Данный подход очень удобен в использовании, но в фон может быть отправлено неограниченное количество операций, при этом все операции начнут выполнение незамедлительно.
Бывают случаи, когда нужно контролировать потребление ресурсов сервера. Самый простой пример — это ограничения на оперативную память, то есть приложение не должно превысить заданный порог потребления. В таком случае нужно каким-то образом ограничить одновременное выполнение задач в фоне.

В указанной выше статье для таких целей использовался кастомный планировщик. Полагаю многим пришла в голову мысль о возможности создании решения на основе SemaphoreSlim, а я же решил его воплотить и проверить насколько проще такое решение, чем кастомный планировщик.

## Первое решение задачи. 
В первую очередь нацеленно на быстроту воплощения и малый объём кода. Оно не обладает гибкостью, к примеру нет контроля над очередью выполнения задач, отправленных в фон.
```csharp
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
```

Вся логика кроется в классе **BackgroundProcessor**. Ограничителем количества одновременно выполняемых задач является **SemaphoreSlim**, его параметры инициализации можно задать через конфигурацию при старте приложения.
```csharp
await _semaphoreSlim.WaitAsync(cancellationToken);
await task.Invoke();
```
Задача, отправленная в фон, начнёт выполняться только тогда, когда в семафоре появиться «свободное место». Количество «свободных место» можно посмотреть через свойство CurrentCount и при необходимости вывести наружу.

## Второе решение. 
Оно чуть сложнее и имеет другой характер использования.
```csharp
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
```

Данный обработчик позволяет отправить в фон единоразово только количество задач равное «свободному месту» в семафоре, в случае его отсутствия вызывающий метод доложен дождаться освобождения семафора.
```csharp
public async Task AwaitProceed(Func<Task> task, CancellationToken cancellationToken)
{
    await _semaphoreSlimQueue.WaitAsync(cancellationToken);

    _ = Task.Run(async () =>
    ***
```
Такой подход достигается другим расположением блокировки семафора - сразу после входа в метод выполняется ожидание семафора, который так же как и до этого ограничивает параллелизм.
При этом есть нюансы использования, к примеру несколько потребителей этого метода будут конкурировать за взятие блокировки (если такой термин уместен) у семафора, то есть один из потребителей может ожидать своей очереди дольше остальных.

Что вы думаете о таком подходе к ограничению параллелизма фоновых задач?