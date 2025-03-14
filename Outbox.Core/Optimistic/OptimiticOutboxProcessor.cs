using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outbox.Abstractions.Senders;
using Outbox.Core.Metrics;
using Outbox.Core.Options;
using Outbox.Core.Repositories;

namespace Outbox.Core.Optimistic;

public interface IOptimiticOutboxProcessor
{
    Task<bool> SendMessages();
    Task<bool> SendMessages(int reminder);
}

public class OptimiticOutboxProcessor : IOptimiticOutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxMessageSender _sender;
    private readonly ILogger<OptimiticOutboxProcessor> _logger;
    private readonly IOutboxMetricsContainer _outboxMetrics;
    private readonly IOptionsMonitor<OptimisticOptions> _options;

    public OptimiticOutboxProcessor(IOutboxRepository outboxRepository, IOutboxMessageSender sender, ILogger<OptimiticOutboxProcessor> logger, IOutboxMetricsContainer outboxMetrics, IOptionsMonitor<OptimisticOptions> options)
    {
        _outboxRepository = outboxRepository;
        _sender = sender;
        _logger = logger;
        _outboxMetrics = outboxMetrics;
        _options = options;
    }


    public async Task<bool> SendMessages()
    {
        var outboxMessage = await _outboxRepository.GetFirstMessage(_options.CurrentValue.RandomRange);
        _outboxMetrics.AddProduceTry();

        if (outboxMessage is null)
        {
            _outboxMetrics.AddError();
            return true;
        }

        await _sender.Send(outboxMessage);
        _outboxMetrics.AddProduced();

        await _outboxRepository.DeleteMessagesByIdAndState([outboxMessage.Id]);

        return true;
    }

    public async Task<bool> SendMessages(int reminder)
    {
        var outboxMessage = await _outboxRepository.GetFirstMessageByReminder(_options.CurrentValue.RandomRange, _options.CurrentValue.Reminders, reminder);
        _outboxMetrics.AddProduceTry();

        if (outboxMessage is null)
        {
            _outboxMetrics.AddError();
            return true;
        }

        await _sender.Send(outboxMessage);
        _outboxMetrics.AddProduced();

        await _outboxRepository.DeleteMessagesByIdAndState([outboxMessage.Id]);

        return true;
    }
}