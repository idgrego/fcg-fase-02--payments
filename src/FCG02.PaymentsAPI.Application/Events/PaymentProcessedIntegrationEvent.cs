namespace FCG02.PaymentsAPI.Application.Events;

public record PaymentProcessedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Amount,
    string Status,
    string? Reason,
    DateTime ProcessedAt
);
