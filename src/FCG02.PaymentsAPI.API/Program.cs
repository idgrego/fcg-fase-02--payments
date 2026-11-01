using FCG02.PaymentsAPI.API.Endpoints;
using FCG02.PaymentsAPI.Application.Extensions;
using FCG02.PaymentsAPI.Infrastructure.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Injeção de Dependências das Camadas de Aplicação e Infraestrutura (EF Core / Npgsql)
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Expõe a especificação em /openapi/v1.json
    app.MapOpenApi();

    // Expõe a interface interativa do Scalar em /scalar/v1
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FIAP Cloud Games - PaymentsAPI")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseHttpsRedirection();

app.MapPaymentEndpoints();
app.MapGet("/health", () => Results.Ok(new
{
    status = "Health",
    service = "PaymentAPI",
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck")
.WithTags("Health");

app.Run();

