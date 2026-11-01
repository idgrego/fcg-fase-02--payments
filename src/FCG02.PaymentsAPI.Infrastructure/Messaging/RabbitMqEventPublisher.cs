using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FCG02.PaymentsAPI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FCG02.PaymentsAPI.Infrastructure.Messaging;

public class RabbitMqEventPublisher
    : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public RabbitMqEventPublisher(
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMqEventPublisher> logger
    )
    {
        this._settings = options.Value;
        this._logger = logger;
        this._factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            AutomaticRecoveryEnabled = true
        };
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _connection.IsOpen)
            return _connection;

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is not null && _connection.IsOpen)
                return _connection;

            _logger.LogInformation("[RabbitMQ Publisher] Conectando ao broker em {Host}:{Port}...",
                _settings.HostName, _settings.Port);

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        string routingKey,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // decalração idempotente da exchange topi de pagamentos
            await channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken
            );

            // Serialização do payload JSON
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(@event, jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            // metadados e persistência da mensagem AMQP
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Type = typeof(TEvent).Name,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            // publicação assícrona
            await channel.BasicPublishAsync(
                exchange: _settings.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("[RabbitMQ Publisher] Evento '{EventName}' publicado com sucesso na exchange '{Exchange}' (RoutingKey: '{RoutingKey}')",
                typeof(TEvent).Name, _settings.Exchange, routingKey);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[RabbitMQ Publisher] Falha ao publicar evento '{EventName}' na exchange '{Exchange}'.",
                typeof(TEvent).Name, _settings.Exchange);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        _connectionLock.Dispose();
        GC.SuppressFinalize(this);
    }
}