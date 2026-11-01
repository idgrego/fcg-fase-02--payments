using FCG02.PaymentsAPI.Domain.Entities;

namespace FCG02.PaymentsAPI.Application.DTOs;

public record PaymentResponse(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Amount,
    string Method,
    string Status,
    string? Reason,
    DateTime CreatedAt,
    DateTime? ProcessedAt
)
{
    public static PaymentResponse FromEntity(PaymentTransaction transaction) =>
        new(
            transaction.Id,
            transaction.OrderId,
            transaction.UserId,
            transaction.GameId,
            transaction.Amount,
            transaction.Method.ToString(),
            transaction.Status.ToString(),
            transaction.Reason,
            transaction.CreatedAt,
            transaction.ProcessedAt
        );
}
