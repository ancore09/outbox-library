using Microsoft.Extensions.Logging;
using Outbox.Abstractions.Senders;
using Outbox.Core.Metrics;
using Outbox.Core.Repositories;

namespace Outbox.Core.Pessimistic;

public interface IPessimisticOutboxProcessor
{
    Task<bool> SendMessages(int batchSize);
}

public class PessimisticOutboxProcessor : IPessimisticOutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxMessageSender _sender;
    private readonly ILogger<PessimisticOutboxProcessor> _logger;
    private readonly IOutboxMetricsContainer _outboxMetrics;

    public PessimisticOutboxProcessor(IOutboxRepository outboxRepository, IOutboxMessageSender sender, ILogger<PessimisticOutboxProcessor> logger, IOutboxMetricsContainer outboxMetrics)
    {
        _outboxRepository = outboxRepository;
        _sender = sender;
        _logger = logger;
        _outboxMetrics = outboxMetrics;
    }


    public async Task<bool> SendMessages(int batchSize)
    {
        var messages = await _outboxRepository.GetMessagesWithLock(batchSize);

        if (messages.Count is 0)
            return false;

        foreach (var outboxMessage in messages)
        {
            await _sender.Send(outboxMessage);
            _outboxMetrics.AddProduced();
        }

        await _outboxRepository.DeleteMessagesByIdAndState(messages.Select(x => x.Id).ToList());

        return true;
    }
}