using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outbox.Abstractions.Models;

namespace Outbox.Core.Leasing;

public interface IWorkerTasksContainer : IDisposable
{
    Task AddOrUpdateTask(WorkerTask config);
    Task CancelAndRemoveTask(string topic);
    Task<List<WorkerTask>> GetWorkerTasks();
}

public class WorkerTasksContainer : IWorkerTasksContainer
{
    private readonly ConcurrentDictionary<string, (WorkerTask Config, Task Task, CancellationTokenSource Cts)> _container = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _globalCts = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkerTasksContainer> _logger;

    public WorkerTasksContainer(ILogger<WorkerTasksContainer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task AddOrUpdateTask(WorkerTask config)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_container.TryGetValue(config.Topic, out var existing))
            {
                await existing.Cts.CancelAsync();
                await existing.Task;
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);

            var task = Task.Run(async () =>
            {
                try
                {
                    // if (config.Topic == "test5")
                    //     throw new Exception("Test exception");
                    
                    while (!cts.Token.IsCancellationRequested)
                    {
                        bool processed = false;
                        do
                        {
                            processed = await ProcessTask(config);
                            await Task.Delay(config.DelayMilliseconds, cts.Token);
                        } while (processed);
                        // await service.SendMessages(config);
                        await Task.Delay(config.DelayMilliseconds, cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Task {Topic} cancelled", config.Topic);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing task {Topic}", config.Topic);
                    await CancelAndRemoveTask(config.Topic);
                }
            }, cts.Token);

            _container.TryAdd(config.Topic, (config, task, cts));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<bool> ProcessTask(WorkerTask config)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ILeasingOutboxProcessor>();

        return await service.SendMessages(config);
    }

    public async Task CancelAndRemoveTask(string topic)
    {
        if (_container.TryRemove(topic, out var existing))
        {
            await existing.Cts.CancelAsync();
            await existing.Task;
        }
    }

    public async Task<List<WorkerTask>> GetWorkerTasks()
    {
        return _container.Values.Select(x => x.Config).ToList();
    }

    public void Dispose()
    {
        _globalCts.Cancel();
        _semaphore.Dispose();
        foreach (var (_, (_, cts, _)) in _container)
        {
            cts.Dispose();
        }
        _globalCts.Dispose();
    }
}