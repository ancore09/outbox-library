using System.Diagnostics.Metrics;

namespace Outbox.Core.Metrics;

public class OutboxMetricsContainer : IOutboxMetricsContainer
{
    private readonly Gauge<long> _mechanism;
    private readonly Gauge<long> _taskEvents;
    private readonly Counter<long> _produced;
    private readonly Counter<long> _concurrencyException;
    private readonly Counter<long> _produceTries;
    private readonly Histogram<long> _produceMills;
    private readonly Histogram<long> _pgMills;

    public OutboxMetricsContainer(IMeterFactory factory)
    {
        var producedMeter = factory.Create("Outbox");
        _mechanism = producedMeter.CreateGauge<long>("outbox_mechanism_started", description: "Used mechanism of outbox processing");
        _taskEvents = producedMeter.CreateGauge<long>("outbox_task_events", description: "Events of outbox processing");
        _produced = producedMeter.CreateCounter<long>("outbox_produced", description: "Number of produced messages");
        _concurrencyException = producedMeter.CreateCounter<long>("outbox_concurrency_error", description: "Number of concurrency errors");
        _produceTries = producedMeter.CreateCounter<long>("outbox_produce_tries", description: "Number of produce_tries");
        _pgMills = producedMeter.CreateHistogram<long>("outbox_pg_mills", description: "Mills of pg");
        _produceMills = producedMeter.CreateHistogram<long>("outbox_produce_mills", description: "Mills of produce");
    }

    public void AddUsedMechanism(string mechanism)
    {
        _mechanism.Record(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new KeyValuePair<string, object?>("mechanism", mechanism));
    }
    
    public void AddTaskEvent(string @event, string[] topics)
    {
        _taskEvents.Record(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
            new KeyValuePair<string, object?>("event", @event),
            new KeyValuePair<string, object?>("topics", string.Join(',', topics))
        );
    }

    public void AddProduced()
    {
        _produced.Add(1);
    }

    public void AddProduceTry()
    {
        _produceTries.Add(1);
    }
    
    public void AddProduceTime(long mills)
    {
        _produceMills.Record(mills);
    }

    public void AddPgTime(long mills, string operation)
    {
        _pgMills.Record(mills, new []{new KeyValuePair<string, object?>("operation", operation)});
    }

    public void AddError()
    {
        _concurrencyException.Add(1);
    }
}