using Microsoft.Extensions.Logging.Abstractions;
using Outbox.Abstractions.Senders;
using Outbox.Core.Options;

namespace Outbox.Playground;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.Section).Get<DatabaseOptions>();
        Console.WriteLine(databaseOptions!.ConnectionString);
        services.AddNpgsqlDataSource(databaseOptions!.ConnectionString, builder =>
        {
            builder.UseLoggerFactory(NullLoggerFactory.Instance);
        });

        return services;
    }
    
    public static IServiceCollection AddProducers(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SenderOptions>(configuration.GetSection(SenderOptions.Section));

        services.AddSingleton<IOutboxMessageSender, KafkaProducer>();

        return services;
    }
}