using Outbox.Abstractions.Models;

namespace Outbox.Core.Repositories;

public interface IOutboxRepository
{
    Task<List<OutboxMessage>> GetMessagesWithLock(int batchSize);
    Task<OutboxMessage?> GetFirstMessage(int randomRange);
    Task<OutboxMessage?> GetFirstMessageByReminder(int randomRange, int remindersCount, int reminder);
    Task<List<OutboxMessage>> GetMessages(string topic, int batchSize);
    Task<int> DeleteMessagesByIdAndState(List<long> idents);
    Task<int> DeleteMessagesByIdAndTopic(List<long> idents, string topic);
    Task InsertMessages(List<OutboxMessage> messages);
}