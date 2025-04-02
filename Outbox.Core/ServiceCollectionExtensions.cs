using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Abstractions.Models;
using Outbox.Core.Leasing;
using Outbox.Core.Metrics;
using Outbox.Core.Optimistic;
using Outbox.Core.Options;
using Outbox.Core.Pessimistic;

namespace Outbox.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOutboxMetricsContainer, OutboxMetricsContainer>();

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section));
        
        services.Configure<LeasingOptions>(configuration.GetSection(LeasingOptions.Section));
        services.Configure<PessimisticOptions>(configuration.GetSection(PessimisticOptions.Section));
        services.Configure<OptimisticOptions>(configuration.GetSection(OptimisticOptions.Section));

        var outboxOptionsSection = configuration.GetSection(OutboxOptions.Section);

        var outboxOptions = outboxOptionsSection.Get<OutboxOptions>();

        switch (outboxOptions!.Type)
        {
            case OutboxType.Leasing:
                services.AddLeasing();
                break;
            case OutboxType.Pessimistic:
                services.AddPessimistic();
                break;
            case OutboxType.Optimistic:
                services.AddOptimistic();
                break;
        }

        services.Configure<OutboxOptions>(outboxOptionsSection);

        return services;
    }

    private static IServiceCollection AddLeasing(this IServiceCollection services)
    {
        services.AddScoped<INewTaskAcquirerService, NewTaskAcquirerService>();
        services.AddSingleton<IWorkerTasksContainer, WorkerTasksContainer>();
        services.AddScoped<ILeaseProlongationService, LeaseProlongationService>();
        services.AddScoped<ILeasingOutboxProcessor, LeasingLeasingOutboxProcessor>();
        
        services.AddHostedService<NewTaskAcquirerBackgroundService>();
        services.AddHostedService<LeaseProlongationBackgroundService>();

        return services;
    }

    private static IServiceCollection AddPessimistic(this IServiceCollection services)
    {
        services.AddScoped<IPessimisticOutboxProcessor, PessimisticOutboxProcessor>();
        services.AddHostedService<PessimisticBackgroundService>();

        return services;
    }

    private static IServiceCollection AddOptimistic(this IServiceCollection services)
    {
        services.AddScoped<IOptimiticOutboxProcessor, OptimiticOutboxProcessor>();
        services.AddHostedService<OptimisticBackgroundService>();

        return services;
    }
}