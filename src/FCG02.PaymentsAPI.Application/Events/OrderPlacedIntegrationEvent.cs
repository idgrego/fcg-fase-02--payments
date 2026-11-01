namespace FCG02.PaymentsAPI.Application.Events;

public record OrderPlacedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price
);
