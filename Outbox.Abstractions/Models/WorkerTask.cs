namespace Outbox.Abstractions.Models;

public class WorkerTask
{
    public required long Id { get; set; }
    public required string Topic { get; set; }
    public DateTimeOffset? LeaseEnd { get; set; }
    public required int BatchSize { get; set; }
    public required int DelayMilliseconds { get; set; }

    public bool IsLeaseExpired()
    {
        return LeaseEnd <= DateTimeOffset.UtcNow;
    }
}