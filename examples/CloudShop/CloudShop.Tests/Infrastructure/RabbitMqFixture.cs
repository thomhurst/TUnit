using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace CloudShop.Tests.Infrastructure;

/// <summary>
/// Provides direct RabbitMQ access for messaging verification tests.
/// Nested dependency: injects DistributedAppFixture to get the connection string.
/// </summary>
public class RabbitMqFixture : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<DistributedAppFixture>(Shared = SharedType.PerTestSession)]
    public required DistributedAppFixture App { get; init; }

    private IConnection? _connection;

    public IConnection Connection => _connection
        ?? throw new InvalidOperationException("RabbitMQ not initialized");

    public async Task InitializeAsync()
    {
        var connectionString = await App.GetConnectionStringAsync("rabbitmq");
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = await factory.CreateConnectionAsync();
    }

    /// <summary>
    /// Subscribe to a RabbitMQ exchange and collect messages of type T.
    /// Returns a MessageCollector that can be awaited for messages.
    /// </summary>
    public async Task<MessageCollector<T>> SubscribeAsync<T>(string exchange, string routingKeyPattern)
    {
        var channel = await Connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);
        var queue = await channel.QueueDeclareAsync(exclusive: true);
        await channel.QueueBindAsync(queue.QueueName, exchange, routingKeyPattern);

        var collector = new MessageCollector<T>();
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<T>(json);
            if (message is not null)
                collector.Add(message);
            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer: consumer);
        return collector;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}

/// <summary>
/// Collects messages from a RabbitMQ subscription and allows waiting for them.
/// </summary>
public class MessageCollector<T>
{
    private readonly List<T> _messages = [];
    private readonly SemaphoreSlim _semaphore = new(0);

    public IReadOnlyList<T> Messages => _messages;

    public void Add(T message)
    {
        lock (_messages) _messages.Add(message);
        _semaphore.Release();
    }

    public async Task<T> WaitForFirstAsync(TimeSpan timeout)
    {
        if (!await _semaphore.WaitAsync(timeout))
            throw new TimeoutException($"No message received within {timeout}");
        lock (_messages) return _messages[^1];
    }
}
