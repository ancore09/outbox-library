namespace Outbox.Core.Options;

public class OptimisticOptions
{
    public static string Section => nameof(OptimisticOptions);

    public int ThrottlingMilliseconds { get; set; }
    public int DelaySeconds { get; set; }

    public int Workers { get; set; }
    public int Reminders { get; set; }
    public int RandomRange { get; set; }
}