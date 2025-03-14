using Outbox.Abstractions.Models;

namespace Outbox.Abstractions.Senders;

public interface IOutboxMessageSender : IDisposable
{
    Task Send(OutboxMessage message);
}