using FCG02.PaymentsAPI.Application.Interfaces;
using FCG02.PaymentsAPI.Domain.Interfaces;
using FCG02.PaymentsAPI.Infrastructure.Data;
using FCG02.PaymentsAPI.Infrastructure.Messaging;
using FCG02.PaymentsAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG02.PaymentsAPI.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // recupera a strng de conexão do Azure SQL
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não foi configurada no appsettings.json");

        // configura o DbContext com SQL Server
        services.AddDbContext<PaymentsDbContext>(options => options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

        // registra o repositório concreto
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Configurações e Publicador de Mensageria (RabbitMQ 7.x)
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMQ"));
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // 4. Consumidor em Segundo Plano (BackgroundService)
        services.AddHostedService<OrderPlacedConsumerService>();

        return services;
    }
}