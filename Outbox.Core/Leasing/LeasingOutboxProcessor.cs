using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Outbox.Abstractions.Models;
using Outbox.Abstractions.Senders;
using Outbox.Core.Metrics;
using Outbox.Core.Repositories;

namespace Outbox.Core.Leasing;

public interface ILeasingOutboxProcessor
{
    Task<bool> SendMessages(WorkerTask config);
}

public class LeasingLeasingOutboxProcessor : ILeasingOutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxMessageSender _sender;
    private readonly ILogger<LeasingLeasingOutboxProcessor> _logger;
    private readonly IOutboxMetricsContainer _outboxMetrics;

    public LeasingLeasingOutboxProcessor(IOutboxRepository outboxRepository, IOutboxMessageSender sender, ILogger<LeasingLeasingOutboxProcessor> logger, IOutboxMetricsContainer outboxMetrics)
    {
        _outboxRepository = outboxRepository;
        _sender = sender;
        _logger = logger;
        _outboxMetrics = outboxMetrics;
    }

    public async Task<bool> SendMessages(WorkerTask config)
    {
        if (config.IsLeaseExpired())
        {
            _logger.LogInformation("lease ended for {Topic}", config.Topic);
            return false;
        }
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var messages = await _outboxRepository.GetMessages(config.Topic, config.BatchSize);
        
        var pgTime = stopwatch.ElapsedMilliseconds;
        _outboxMetrics.AddPgTime(pgTime, "fetch");
        stopwatch.Stop();
        stopwatch.Reset();
        
        if (messages.Count is 0)
            return false;

        foreach (var outboxMessage in messages)
        {
            stopwatch.Start();
            
            await _sender.Send(outboxMessage);
            
            stopwatch.Stop();
            var mills = stopwatch.ElapsedMilliseconds;
            _outboxMetrics.AddProduced();
            _outboxMetrics.AddProduceTime(mills);
            
            stopwatch.Reset();
        }

        stopwatch.Start();
        
        await _outboxRepository.DeleteMessagesByIdAndTopic(messages.Select(x => x.Id).ToList(), config.Topic);
        pgTime = stopwatch.ElapsedMilliseconds;
        _outboxMetrics.AddPgTime(pgTime, "delete");
        stopwatch.Stop();
        stopwatch.Reset();

        return true;
    }
}