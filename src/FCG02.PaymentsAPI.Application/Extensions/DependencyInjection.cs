using FCG02.PaymentsAPI.Application.Interfaces;
using FCG02.PaymentsAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FCG02.PaymentsAPI.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPaymentGatewaySimulator, PaymentGatewaySimulator>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
