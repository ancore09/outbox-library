namespace Outbox.Core.Options;

public class PessimisticOptions
{
    public static string Section => nameof(PessimisticOptions);

    public int ThrottlingMilliseconds { get; set; }
    public int DelaySeconds { get; set; }

    public int BatchSize { get; set; }
    public int Workers { get; set; }
}