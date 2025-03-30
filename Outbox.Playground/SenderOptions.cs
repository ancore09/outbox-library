namespace Outbox.Playground;

public class SenderOptions
{
    public static string Section => nameof(SenderOptions);

    public required string Server { get; set; }
    public required string ClientId { get; set; }
}