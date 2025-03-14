using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Core;
using Outbox.Core.Repositories;
using Outbox.Dapper.Repositories;

namespace Outbox.Dapper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOutboxCore(configuration);
        
        services.AddScoped<IWorkerTaskRepository, WorkerTaskRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        
        return services;
    }
}