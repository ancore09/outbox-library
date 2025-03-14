using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Outbox.Abstractions.Models;
using Outbox.Core.Repositories;

namespace Outbox.Dapper.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<OutboxRepository> _logger;

    public OutboxRepository(NpgsqlConnection connection, ILogger<OutboxRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task InsertMessages(List<OutboxMessage> messages)
    {
        var insertQuery = """
                          insert into outbox(key, payload, topic, state)
                          values (@Key, @Payload, @Topic, @State)
                          """;

        await _connection.ExecuteAsync(insertQuery, messages);
    }

    public async Task<List<OutboxMessage>> GetMessagesWithLock(int batchSize)
    {
        if (_connection.State is not ConnectionState.Open)
            await _connection.OpenAsync();

        await using var transaction = await _connection.BeginTransactionAsync();

        var selectQuery = """
                    select * from outbox
                    where state = 0
                    order by id
                    limit @batchSize
                    for update skip locked;
                    """;

        var result = (await _connection.QueryAsync<OutboxMessage>(selectQuery, new { batchSize = batchSize })).ToList();

        var updateQuery = """
                          update outbox
                          set state = 1
                          where id = ANY(@idents)
                          """;

        await _connection.ExecuteAsync(updateQuery, new { idents = result.Select(x => x.Id).ToArray() });

        await transaction.CommitAsync();

        return result;
    }

    public async Task<OutboxMessage?> GetFirstMessage(int randomRange)
    {
        var selectQuery = """
                          select *, xmin from outbox
                          where state = 0
                          order by id
                          limit @randomRange;
                          """;

        var result = (await _connection.QueryAsync<OutboxMessage>(selectQuery, new { randomRange = randomRange })).ToList();

        if (result.Count is 0)
            return null;

        // var randIndex = new Random(new Guid().GetHashCode()).Next(result.Count);
        var randIndex = Enumerable.Range(1, 3).Aggregate((i, i1) => Random.Shared.Next(1, result.Count));

        var message = result[randIndex % result.Count];

        var updateQuery = """
                          update outbox
                          set state = 1
                          where id = @id and state = 0 and xmin = @xmin
                          """;

        var updated = await _connection.ExecuteAsync(updateQuery, new { id = message.Id, xmin = message.Xmin });

        // await _connection.CloseAsync();

        if (updated is 0)
        {
            _logger.LogInformation("Optimistic concurrency exception");
            return null;
        }

        return message;
    }
    
    public async Task<OutboxMessage?> GetFirstMessageByReminder(int randomRange, int remindersCount, int reminder)
    {
        var selectQuery = """
                          select *, xmin from outbox
                          where state = 0 and id % @remindersCount = @reminder
                          order by id
                          limit @randomRange;
                          """;

        var result = (await _connection.QueryAsync<OutboxMessage>(selectQuery, new { randomRange = randomRange, remindersCount = remindersCount, reminder = reminder })).ToList();
        
        if (result.Count is 0)
            return null;
        
        var message = result[Random.Shared.Next(0, result.Count)];

        var updateQuery = """
                          update outbox
                          set state = 1
                          where id = @id and state = 0 and xmin = @xmin
                          """;

        var updated = await _connection.ExecuteAsync(updateQuery, new { id = message.Id, xmin = message.Xmin });

        // await _connection.CloseAsync();

        if (updated is 0)
        {
            _logger.LogInformation("Optimistic concurrency exception");
            return null;
        }

        return message;
    }

    public async Task<List<OutboxMessage>> GetMessages(string topic, int batchSize)
    {
        var query = """
                    select * from outbox
                    where topic = @topic
                    order by id
                    limit @batchSize;
                    """;

        var result = await _connection.QueryAsync<OutboxMessage>(query, new { topic = topic, batchSize = batchSize });

        return result.ToList();
    }

    public async Task<int> DeleteMessagesByIdAndState(List<long> idents)
    {
        var query = """
                    delete from outbox
                    where state = 1 and id = ANY (@idents);
                    """;

        var result = await _connection.ExecuteAsync(query, new { idents = idents });

        return result;
    }

    public async Task<int> DeleteMessagesByIdAndTopic(List<long> idents, string topic)
    {
        var query = """
                    delete from outbox
                    where topic = @topic and id = ANY (@idents);
                    """;

        var result = await _connection.ExecuteAsync(query, new { topic = topic, idents = idents });

        return result;
    }

    public async Task<int> InsertProduced(List<OutboxMessage> messages)
    {
        var query = """
                    insert into produced (topic, payload)
                    values (@Topic, @Payload)
                    """;

        var result = await _connection.ExecuteAsync(query, messages);

        return result;
    }
}