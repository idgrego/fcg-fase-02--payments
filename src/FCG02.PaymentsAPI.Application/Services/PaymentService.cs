using FCG02.PaymentsAPI.Application.DTOs;
using FCG02.PaymentsAPI.Application.Events;
using FCG02.PaymentsAPI.Application.Interfaces;
using FCG02.PaymentsAPI.Domain.Entities;
using FCG02.PaymentsAPI.Domain.Enums;
using FCG02.PaymentsAPI.Domain.Interfaces;

namespace FCG02.PaymentsAPI.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentGatewaySimulator _gatewaySimulator;
    private readonly IEventPublisher _eventPublisher;

    public PaymentService(
        IPaymentRepository repository,
        IPaymentGatewaySimulator gatewaySimulator,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _gatewaySimulator = gatewaySimulator;
        _eventPublisher = eventPublisher;
    }

    public async Task<PaymentResponse> ProcessOrderPaymentAsync(OrderPlacedIntegrationEvent orderEvent, CancellationToken cancellationToken = default)
    {
        // 1. Proteção de Idempotência: Verifica se o OrderId já foi recebido anteriormente
        var existingTransaction = await _repository.GetByOrderIdAsync(orderEvent.OrderId, cancellationToken);
        if (existingTransaction is not null)
        {
            // Se já foi processada (Approved ou Rejected), retorna a resposta sem reprocessar
            if (existingTransaction.Status != PaymentStatus.Pending)
                return PaymentResponse.FromEntity(existingTransaction);
        }

        // 2. Se for nova, cria a transação pendente no domínio
        var transaction = existingTransaction ?? new PaymentTransaction(
            orderId: orderEvent.OrderId,
            userId: orderEvent.UserId,
            gameId: orderEvent.GameId,
            amount: orderEvent.Price,
            method: PaymentMethod.CreditCard
        );

        if (existingTransaction is null)
        {
            await _repository.AddAsync(transaction, cancellationToken);
        }

        // 3. Executa a cobrança no simulador de gateway
        var gatewayResult = await _gatewaySimulator.ProcessAsync(transaction, cancellationToken);

        // 4. Aplica a transição de estado de negócio rica
        if (gatewayResult.IsSuccess)
        {
            transaction.Approve($"Autorização: {gatewayResult.AuthorizationCode}");
        }
        else
        {
            transaction.Reject(gatewayResult.FailureReason ?? "Pagamento não autorizado pela operadora.");
        }

        // 5. Persiste a alteração no repositório
        await _repository.UpdateAsync(transaction, cancellationToken);

        // 6. Publica o evento de integração para RabbitMQ
        var routingKey = transaction.Status == PaymentStatus.Approved ? "payment.approved" : "payment.rejected";

        var integrationEvent = new PaymentProcessedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            OrderId: transaction.OrderId,
            UserId: transaction.UserId,
            GameId: transaction.GameId,
            Amount: transaction.Amount,
            Status: transaction.Status.ToString(),
            Reason: transaction.Reason,
            ProcessedAt: transaction.ProcessedAt ?? DateTime.UtcNow
        );

        await _eventPublisher.PublishAsync(integrationEvent, routingKey, cancellationToken);

        return PaymentResponse.FromEntity(transaction);
    }

    public async Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(id, cancellationToken);
        return transaction is not null ? PaymentResponse.FromEntity(transaction) : null;
    }

    public async Task<PaymentResponse?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByOrderIdAsync(orderId, cancellationToken);
        return transaction is not null ? PaymentResponse.FromEntity(transaction) : null;
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var transactions = await _repository.GetByUserIdAsync(userId, cancellationToken);
        return transactions.Select(PaymentResponse.FromEntity).ToList();
    }
}
