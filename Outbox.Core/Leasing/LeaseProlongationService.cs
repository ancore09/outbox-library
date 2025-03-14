using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outbox.Core.Options;
using Outbox.Core.Repositories;

namespace Outbox.Core.Leasing;

public interface ILeaseProlongationService
{
    Task TryProlongLeases();
}

public class LeaseProlongationService : ILeaseProlongationService
{
    private readonly IWorkerTaskRepository _workerTaskRepository;
    private readonly IWorkerTasksContainer _container;
    private readonly ILogger<LeaseProlongationService> _logger;
    private readonly IOptionsMonitor<LeasingOptions> _options;

    public LeaseProlongationService(IWorkerTaskRepository workerTaskRepository, IWorkerTasksContainer container, ILogger<LeaseProlongationService> logger, IOptionsMonitor<LeasingOptions> options)
    {
        _workerTaskRepository = workerTaskRepository;
        _container = container;
        _logger = logger;
        _options = options;
    }

    public async Task TryProlongLeases()
    {
        var tasks = await _container.GetWorkerTasks();

        var tasksToProlong = tasks
            .Where(x => DateTimeOffset.UtcNow.AddSeconds(_options.CurrentValue.TaskProlongationThresholdSeconds) > x.LeaseEnd)
            .ToList();

        if (tasksToProlong.Count is 0)
            return;

        var updateQuery = tasksToProlong.
            Select(x => (x, x.LeaseEnd!.Value.AddMinutes(_options.CurrentValue.LeaseDurationMinutes)))
            .ToList();

        await _workerTaskRepository.UpdateLeases(updateQuery);

        foreach (var (workerTask, leaseEnd) in updateQuery)
        {
            workerTask.LeaseEnd = leaseEnd;
            _logger.LogInformation("Lease for {task} has been prolonged till {leaseEnd}", workerTask.Topic, workerTask.LeaseEnd);
        }

        _logger.LogInformation("Leases for tasks have been prolonged: {tasks}", (object)tasksToProlong.Select(x => x.Topic).ToArray());
    }
}