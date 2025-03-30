namespace Outbox.Core.Metrics;

public interface IOutboxMetricsContainer
{
    void AddTaskEvent(string @event, string[] topic);
    void AddUsedMechanism(string mechanism);
    void AddProduced();
    void AddProduceTime(long mills);
    void AddPgTime(long mills, string operation);
    void AddError();
    void AddProduceTry();
}