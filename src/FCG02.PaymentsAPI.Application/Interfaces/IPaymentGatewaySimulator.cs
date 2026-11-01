using FCG02.PaymentsAPI.Application.DTOs;
using FCG02.PaymentsAPI.Domain.Entities;

namespace FCG02.PaymentsAPI.Application.Interfaces;

public interface IPaymentGatewaySimulator
{
    Task<PaymentGatewayResult> ProcessAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
}
