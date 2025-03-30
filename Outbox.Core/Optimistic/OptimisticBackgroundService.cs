using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outbox.Core.Metrics;
using Outbox.Core.Options;

namespace Outbox.Core.Optimistic;

public class OptimisticBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<OptimisticOptions> _options;
    private readonly IOutboxMetricsContainer _metricsContainer;

    public OptimisticBackgroundService(IServiceProvider serviceProvider, IOptionsMonitor<OptimisticOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var _logger = _serviceProvider.GetRequiredService<ILogger<OptimisticBackgroundService>>();

        _logger.LogInformation("Optimistic Outbox started");
        _metricsContainer.AddUsedMechanism("optimistic");


        var tasks = Enumerable.Range(1, _options.CurrentValue.Workers).Select(x => StartWork(stoppingToken)).ToList();
        // var tasks = Enumerable.Range(0, _options.CurrentValue.Workers).Select(i => StartWorkByReminder(i, stoppingToken)).ToList();

        await Task.WhenAll(tasks);
    }

    private async Task StartWork(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            bool result = false;

            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.CurrentValue.ThrottlingMilliseconds));

                using var scope = _serviceProvider.CreateScope();
                var scopeServiceProvider = scope.ServiceProvider;
                var service = scopeServiceProvider.GetRequiredService<IOptimiticOutboxProcessor>();

                result = await service.SendMessages();
            } while (result && !stoppingToken.IsCancellationRequested);

            await Task.Delay(TimeSpan.FromSeconds(_options.CurrentValue.DelaySeconds), stoppingToken);
        }
    }
    
    private async Task StartWorkByReminder(int i, CancellationToken stoppingToken)
    {
        await Task.Yield();
        Console.WriteLine(i);

        var reminder = i % _options.CurrentValue.Reminders;

        while (!stoppingToken.IsCancellationRequested)
        {
            bool result = false;

            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.CurrentValue.ThrottlingMilliseconds));

                using var scope = _serviceProvider.CreateScope();
                var scopeServiceProvider = scope.ServiceProvider;
                var service = scopeServiceProvider.GetRequiredService<IOptimiticOutboxProcessor>();

                result = await service.SendMessages(reminder);
            } while (result && !stoppingToken.IsCancellationRequested);

            await Task.Delay(TimeSpan.FromSeconds(_options.CurrentValue.DelaySeconds), stoppingToken);
        }
    }
}