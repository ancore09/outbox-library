using Outbox.Abstractions.Models;

namespace Outbox.Core.Options;

public class OutboxOptions
{
    public static string Section => nameof(OutboxOptions);

    public OutboxType? Type { get; set; }
}