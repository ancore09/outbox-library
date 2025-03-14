namespace Outbox.Abstractions.Models;

public enum OutboxType
{
    None,
    Leasing,
    Pessimistic,
    Optimistic
}