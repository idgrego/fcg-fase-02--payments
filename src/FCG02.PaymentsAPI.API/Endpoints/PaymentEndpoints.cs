using FCG02.PaymentsAPI.Application.DTOs;
using FCG02.PaymentsAPI.Application.Events;
using FCG02.PaymentsAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FCG02.PaymentsAPI.API.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {

        var group = app.MapGroup("/api/v1/payments")
            .WithTags("Payments");

        DefineGetPaymentByOrderId(group);
        DefineGetPaymentById(group);
        DefineGetPaymentByUserId(group);
        DefinePostSimulatePayment(group);

        return app;
    }

    private static void DefinePostSimulatePayment(RouteGroupBuilder group)
    {
        group.MapPost("/simulate", async Task<Results<Created<PaymentResponse>, BadRequest<object>>> (
            OrderPlacedIntegrationEvent request,
            IPaymentService paymentService,
            CancellationToken cancellationToken
        ) =>
        {
            if (request.OrderId == Guid.Empty || request.UserId == Guid.Empty || request.GameId == Guid.Empty)
                return TypedResults.BadRequest((object)new { message = $"OrderId, UserId e GameId são obrigatórios." });
            if (request.Price < 0)
                TypedResults.BadRequest((object)new { message = "O valor do pedido não pode ser negativo." });
            var response = await paymentService.ProcessOrderPaymentAsync(request, cancellationToken);
            return TypedResults.Created($"/api/v1/payments/{response.OrderId}", response);
        })
        .WithName("SimulatePayment")
        .WithSummary("Simula o processamento completo de um pagamento a partir de um pedido.")
        .Produces<PaymentResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static void DefineGetPaymentByUserId(RouteGroupBuilder group)
    {
        group.MapGet("/user/{userId:guid}", async Task<Ok<IReadOnlyList<PaymentResponse>>> (
            Guid userId,
            IPaymentService paymentService,
            CancellationToken cancellationToken
        ) =>
        {
            var payments = await paymentService.GetByUserIdAsync(userId, cancellationToken);
            return TypedResults.Ok(payments);
        })
        .WithName("GetPaymentsByUserId")
        .WithSummary("Lista todas as transações financeiras de um determinado usuário (UserId)")
        .Produces<IReadOnlyList<PaymentResponse>>(StatusCodes.Status200OK);
    }

    private static void DefineGetPaymentById(RouteGroupBuilder group)
    {
        group.MapGet("/transaction/{id:guid}", async Task<Results<Ok<PaymentResponse>, NotFound<object>>> (
            Guid id,
            IPaymentService paymentService,
            CancellationToken cancellationToken
        ) =>
        {
            var payment = await paymentService.GetByIdAsync(id, cancellationToken);
            return payment is null
                ? TypedResults.NotFound((object)new { message = $"Nenhuma transação encontrada com o ID '{id}'." })
                : TypedResults.Ok(payment);
        })
        .WithName("GetPaymentId")
        .WithSummary("Consulta uma transação de pagamento através do seu identificador exclusivo (id)")
        .Produces<PaymentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static void DefineGetPaymentByOrderId(RouteGroupBuilder group)
    {
        group.MapGet("/{orderId:guid}", async Task<Results<Ok<PaymentResponse>, NotFound<object>>> (
            Guid orderId,
            IPaymentService paymentService,
            CancellationToken cancellationToken
        ) =>
        {
            var payment = await paymentService.GetByOrderIdAsync(orderId, cancellationToken);
            if (payment is null)
                return TypedResults.NotFound((object)new { message = $"Nenhum pagamento encontrado para o pedido '{orderId}'." });
            return TypedResults.Ok(payment);
        })
        .WithName("GetPaymentByOrderId")
        .WithSummary("Consulta uma transação de pagamento através do ID do pedido (OrderId)")
        .Produces<PaymentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }

}