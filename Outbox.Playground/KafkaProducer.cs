using System.Diagnostics;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Outbox.Abstractions.Models;
using Outbox.Abstractions.Senders;
using Outbox.Core.Metrics;

namespace Outbox.Playground;

public class KafkaProducer : IOutboxMessageSender
{
    private readonly SenderOptions _options;
    private readonly IProducer<string, string> _producer;
    private readonly IOutboxMetricsContainer _metrics;

    public KafkaProducer(IOptionsMonitor<SenderOptions> options, IOutboxMetricsContainer metrics)
    {
        _metrics = metrics;
        _options = options.CurrentValue;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.Server,
            ClientId = _options.ClientId,
            // MessageTimeoutMs = config.Value.MessageTimeoutMs,
            Acks = Acks.Leader,
            // // Enable idempotence for exactly-once delivery semantics
            // EnableIdempotence = true,
            // // Increase reliability
            // MessageSendMaxRetries = 3,
            // RetryBackoffMs = 1000,
            LingerMs = 0,
            EnableDeliveryReports = true
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .Build();
    }

    public async Task Send(OutboxMessage message)
    {
        var msg = new Message<string, string>()
        {
            Key = message.Key,
            Value = message.Payload
        };
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        await _producer.ProduceAsync(message.Topic, msg);
        
        stopwatch.Stop();
        var mills = stopwatch.ElapsedMilliseconds;
        _metrics.AddProduced();
        _metrics.AddProduceTime(mills);
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}