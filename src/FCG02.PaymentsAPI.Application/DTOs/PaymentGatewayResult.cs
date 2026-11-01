namespace FCG02.PaymentsAPI.Application.DTOs;

public record PaymentGatewayResult(
    bool IsSuccess,
    string? AuthorizationCode,
    string? FailureReason
);