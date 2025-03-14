namespace Outbox.Core.Metrics;

public interface IOutboxMetricsContainer
{
    void AddProduced();
    void AddProduceTime(long mills);
    void AddPgTime(long mills, string operation);
    void AddError();
    void AddProduceTry();
}