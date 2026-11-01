using FCG02.PaymentsAPI.Application.DTOs;
using FCG02.PaymentsAPI.Application.Interfaces;
using FCG02.PaymentsAPI.Domain.Entities;

namespace FCG02.PaymentsAPI.Application.Services;

/// <summary>
/// Simulador determinístico de adquirente bancária e análise antifraude.
/// </summary>
public class PaymentGatewaySimulator : IPaymentGatewaySimulator
{
    public Task<PaymentGatewayResult> ProcessAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        // 1. Jogos Gratuitos: aprovação imediata
        if (transaction.Amount == 0m)
        {
            return Task.FromResult(new PaymentGatewayResult(
                IsSuccess: true,
                AuthorizationCode: "FREE-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                FailureReason: null
            ));
        }

        // 2. Regra de Limite Máximo / Antifraude (Transações acima de R$ 5.000)
        if (transaction.Amount >= 5000m)
        {
            return Task.FromResult(new PaymentGatewayResult(
                IsSuccess: false,
                AuthorizationCode: null,
                FailureReason: "Transação bloqueada pelas políticas de risco antifraude (limite de R$ 5.000,00 excedido)."
            ));
        }

        // 3. Regra Determinística de Teste de Recusa: Valor R$ 999.99 (Saldo Insuficiente)
        if (transaction.Amount == 999.99m)
        {
            return Task.FromResult(new PaymentGatewayResult(
                IsSuccess: false,
                AuthorizationCode: null,
                FailureReason: "Saldo ou limite de crédito insuficiente no cartão informado."
            ));
        }

        // 4. Regra Determinística de Teste de Recusa: Valor R$ 888.88 (Cartão Expirado / Bloqueado)
        if (transaction.Amount == 888.88m)
        {
            return Task.FromResult(new PaymentGatewayResult(
                IsSuccess: false,
                AuthorizationCode: null,
                FailureReason: "Cartão expirado ou temporariamente bloqueado pelo emissor."
            ));
        }

        // 5. Fluxo Padrão: Transação aprovada com código de autorização gerado
        var authCode = "AUTH-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        return Task.FromResult(new PaymentGatewayResult(
            IsSuccess: true,
            AuthorizationCode: authCode,
            FailureReason: null
        ));
    }
}
