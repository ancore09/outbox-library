using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outbox.Core.Metrics;
using Outbox.Core.Options;

namespace Outbox.Core.Leasing;

public class NewTaskAcquirerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<LeasingOptions> _leasingOptions;
    private readonly IOutboxMetricsContainer _metricsContainer;

    public NewTaskAcquirerBackgroundService(IServiceProvider serviceProvider, IOptionsMonitor<LeasingOptions> leasingOptions, IOutboxMetricsContainer metricsContainer)
    {
        _serviceProvider = serviceProvider;
        _leasingOptions = leasingOptions;
        _metricsContainer = metricsContainer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var _logger = _serviceProvider.GetRequiredService<ILogger<NewTaskAcquirerBackgroundService>>();

        _logger.LogInformation("Leasing Outbox started");
        
        _metricsContainer.AddUsedMechanism("leasing");

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