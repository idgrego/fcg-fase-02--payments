using FCG02.PaymentsAPI.Application.DTOs;
using FCG02.PaymentsAPI.Application.Events;

namespace FCG02.PaymentsAPI.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> ProcessOrderPaymentAsync(OrderPlacedIntegrationEvent orderEvent, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentResponse>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
