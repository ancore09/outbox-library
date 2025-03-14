using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outbox.Core.Leasing;
using Outbox.Core.Options;

namespace Outbox.Infrastructure.Leasing;

public class NewTaskAcquirerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<LeasingOptions> _leasingOptions;

    public NewTaskAcquirerBackgroundService(IServiceProvider serviceProvider, IOptionsMonitor<LeasingOptions> leasingOptions)
    {
        _serviceProvider = serviceProvider;
        _leasingOptions = leasingOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var _logger = _serviceProvider.GetRequiredService<ILogger<NewTaskAcquirerBackgroundService>>();

        _logger.LogInformation("Leasing Outbox started");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;

        await StartWork(serviceProvider, stoppingToken);
    }

    private async Task StartWork(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var service = serviceProvider.GetRequiredService<INewTaskAcquirerService>();

            var result = await service.TryAcquireNewTask();

            if (result)
                continue;

            await Task.Delay(TimeSpan.FromSeconds(_leasingOptions.CurrentValue.NewTaskCheckIntervalSeconds), stoppingToken);
        }
    }
}