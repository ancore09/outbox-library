namespace Outbox.Core.Options;

public class LeasingOptions
{
    public static string Section => nameof(LeasingOptions);

    public int ThrottlingMilliseconds { get; set; }
    public int DelaySeconds { get; set; }
    public int NewTaskCheckIntervalSeconds { get; set; }
    public int TaskProlongationCheckIntervalSeconds { get; set; }
    public int TaskProlongationThresholdSeconds { get; set; }
    public int LeaseDurationMinutes { get; set; }
}