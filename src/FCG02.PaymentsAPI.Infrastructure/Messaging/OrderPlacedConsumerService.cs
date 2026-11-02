using System.Text;
using System.Text.Json;
using FCG02.PaymentsAPI.Application.Events;
using FCG02.PaymentsAPI.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FCG02.PaymentsAPI.Infrastructure.Messaging;

public class OrderPlacedConsumerService : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderPlacedConsumerService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public OrderPlacedConsumerService(
        IOptions<RabbitMqSettings> options,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderPlacedConsumerService> logger
    )
    {
        _settings = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("[RabbitMQ Worker] Inicializando consumidor de pedidos em {Host}:{Port}...",
            _settings.HostName, _settings.Port);

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            AutomaticRecoveryEnabled = true
        };

        try
        {
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // declara a exchange do catálogo (caso ainda não criada)
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.CatalogExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken
            );

            // Declara a fila durável da PaymentsAPI
            await _channel.QueueDeclareAsync(
                queue: _settings.OrderPlacedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // Vincula a fila à exchange do catálogo com a routing key 'order.placed'
            await _channel.QueueBindAsync(
                queue: _settings.OrderPlacedQueue,
                exchange: _settings.CatalogExchange,
                routingKey: _settings.OrderPlacedRoutingKey,
                cancellationToken: stoppingToken);

            // Configura o consumidor assíncrono com Confirmação Manual (autoAck: false)
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(
                queue: _settings.OrderPlacedQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogDebug("[RabbitMQ Worker] Consumidor ativo na fila '{Queue}' vinculada à exchange '{Exchange}' (RoutingKey: '{RoutingKey}')",
                _settings.OrderPlacedQueue, _settings.CatalogExchange, _settings.OrderPlacedRoutingKey);

            // Mantém o worker ativo aguardando mensagens até o encerramento do host
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[RabbitMQ Worker] Encerrando o consumidor de pagamentos...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RabbitMQ Worker] Falha crítica ao inicializar o consumidor de pagamentos.");
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs @event)
    {
        var rawBody = @event.Body.ToArray();
        var json = Encoding.UTF8.GetString(rawBody);

        try
        {
            _logger.LogDebug("[RabbitMQ Worker] Pedido recebido! RoutingKey: '{RoutingKey}', DeliveryTag: {Tag}",
                @event.RoutingKey, @event.DeliveryTag);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var orderEvent = JsonSerializer.Deserialize<OrderPlacedIntegrationEvent>(json, options);
            if (orderEvent is null)
            {
                _logger.LogWarning("⚠️ [RabbitMQ Worker] Payload inválido/nulo ignorado: {Json}", json);
                // Rejeita mensagem sem reenfileirar para evitar starvation
                await _channel!.BasicNackAsync(@event.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            _logger.LogInformation("💳 [PaymentsAPI] Microsserviço de pagamentos recebeu o push do pedido {OrderId} (UserId: {UserId}, Jogo: {GameId}, Valor: R$ {Price:F2}).",
                orderEvent.OrderId, orderEvent.UserId, orderEvent.GameId, orderEvent.Price);

            // cria o escopo para resolver o IPaymentService (Scoped)
            using var scope = _scopeFactory.CreateScope();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            // executa o processamento do pagamento
            var response = await paymentService.ProcessOrderPaymentAsync(orderEvent, @event.CancellationToken);

            _logger.LogInformation("✅ [PaymentsAPI] Validações realizadas e pagamento processado com status '{Status}' (Transação: {Id}). Tabela(s) modificada(s): PaymentTransactions.",
                response.Status, response.Id);

            _logger.LogInformation("🚀 [PaymentsAPI] Evento 'PaymentProcessedEvent' postado na fila para recuperação pelo Catálogo e Notificações (RoutingKey: 'payment.{Status}').",
                response.Status.ToLower());

            // Confirma o processamento com sucesso removendo a mensagem da fila
            await _channel!.BasicAckAsync(@event.DeliveryTag, multiple: false, cancellationToken: @event.CancellationToken);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[RabbitMQ Worker] Erro ao processar o Pedido. Payload: {Json}", json);
            // Em caso de falha não tratada, rejeita sem requeue (evita loops infinitos)
            await _channel!.BasicNackAsync(@event.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}