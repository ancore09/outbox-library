using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Outbox.Core.Options;

namespace Outbox.Core.Leasing;

public class LeaseProlongationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<LeasingOptions> _options;

    public LeaseProlongationBackgroundService(IServiceProvider serviceProvider, IOptionsMonitor<LeasingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;

        await StartWork(serviceProvider, stoppingToken);
    }

    private async Task StartWork(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var service = serviceProvider.GetRequiredService<ILeaseProlongationService>();

            await service.TryProlongLeases();

            await Task.Delay(TimeSpan.FromSeconds(_options.CurrentValue.TaskProlongationCheckIntervalSeconds), stoppingToken);
        }
    }
}