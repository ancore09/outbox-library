using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outbox.Core.Options;
using Outbox.Core.Repositories;

namespace Outbox.Core.Leasing;

public interface INewTaskAcquirerService
{
    Task<bool> TryAcquireNewTask();
}

public class NewTaskAcquirerService : INewTaskAcquirerService
{
    private readonly IWorkerTaskRepository _workerTaskRepository;
    private readonly IWorkerTasksContainer _container;
    private readonly ILogger<NewTaskAcquirerService> _logger;
    private readonly IOptionsMonitor<LeasingOptions> _options;

    public NewTaskAcquirerService(IWorkerTaskRepository workerTaskRepository, IWorkerTasksContainer container, ILogger<NewTaskAcquirerService> logger, IOptionsMonitor<LeasingOptions> options)
    {
        _workerTaskRepository = workerTaskRepository;
        _container = container;
        _logger = logger;
        _options = options;
    }

    public async Task<bool> TryAcquireNewTask()
    {
        await _workerTaskRepository.BeginTransaction();

        try
        {
            var workerTask = await _workerTaskRepository.GetFirstFreeTaskWithLock();

            if (workerTask is null)
            {
                _workerTaskRepository.Rollback();
                return false;
            }

            var leaseEnd = DateTimeOffset.UtcNow.AddMinutes(_options.CurrentValue.LeaseDurationMinutes);
            workerTask.LeaseEnd = leaseEnd;

            await _workerTaskRepository.UpdateLease(workerTask.Id, leaseEnd);

            _workerTaskRepository.Commit();

            _logger.LogInformation("Acquired new task: {task}", workerTask.Topic);

            await _container.AddOrUpdateTask(workerTask);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during acquiring new task");
            _workerTaskRepository.Rollback();
        }

        return false;
    }
}