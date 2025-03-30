using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Outbox.Dapper;
using Outbox.Playground;
using Outbox.Playground.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddYamlFile("config.yaml", optional: false, reloadOnChange: true)
    .AddYamlFile($"config.{builder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "Outbox"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddMeter("Outbox")
    );

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddProducers(builder.Configuration);
builder.Services.AddOutbox(builder.Configuration);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;


var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcReflectionService();
app.MapGrpcService<GreeterService>();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();