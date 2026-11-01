# PaymentsAPI - Documentação Técnica: Camada de Apresentação (API)

A camada **`FCG02.PaymentsAPI.API`** expõe os endpoints HTTP de consulta, auditoria e simulação da aplicação através de **Minimal APIs modulares**, integradas à documentação interativa com **Scalar**.

---

## 📑 Sumário

- [1. Visão Geral das Minimal APIs](#1-visão-geral-das-minimal-apis)
- [2. Mapeamento de Rotas (`PaymentEndpoints`)](#2-mapeamento-de-rotas-paymentendpoints)
  - [2.1. `GET /api/v1/payments/{orderId}` (Busca por Pedido)](#21-get-apiv1paymentsorderid-busca-por-pedido)
  - [2.2. `GET /api/v1/payments/transaction/{id}` (Busca por Transação)](#22-get-apiv1paymentstransactionid-busca-por-transação)
  - [2.3. `GET /api/v1/payments/user/{userId}` (Extrato do Jogador)](#23-get-apiv1paymentsuseruserid-extrato-do-jogador)
  - [2.4. `POST /api/v1/payments/simulate` (Simulação Direta)](#24-post-apiv1paymentssimulate-simulação-direta)
  - [2.5. `GET /health` (HealthCheck)](#25-get-health-healthcheck)
- [3. Documentação Interativa com Scalar](#3-documentação-interativa-com-scalar)
- [4. Pipeline no `Program.cs`](#4-pipeline-no-programcs)
- [5. Navegação](#5-navegação)

---

## 1. Visão Geral das Minimal APIs

A PaymentsAPI adota Minimal APIs modulares do .NET 10:
- Uso de `RouteGroupBuilder` agrupado sob o prefixo `/api/v1/payments`.
- Uso de retornos fortemente tipados com `TypedResults` (`Results<Ok<T>, NotFound<T>>`).
- Separação em métodos privados limpos (`DefineGetPaymentByOrderId`, `DefinePostSimulatePayment`, etc.).

---

## 2. Mapeamento de Rotas (`PaymentEndpoints`)

Localização: `src/FCG02.PaymentsAPI.API/Endpoints/PaymentEndpoints.cs`

### 2.1. `GET /api/v1/payments/{orderId}` (Busca por Pedido)
- **Objetivo:** Permite que clientes ou microsserviços consultem o status financeiro de um pedido gerado no catálogo.
- **Respostas:** `200 OK` (`PaymentResponse`) ou `404 Not Found`.

### 2.2. `GET /api/v1/payments/transaction/{id}` (Busca por Transação)
- **Objetivo:** Recupera os detalhes da transação financeira pelo seu identificador primário.
- **Respostas:** `200 OK` ou `404 Not Found`.

### 2.3. `GET /api/v1/payments/user/{userId}` (Extrato do Jogador)
- **Objetivo:** Retorna a lista completa de pagamentos (aprovados, pendentes e rejeitados) associados a um jogador.
- **Respostas:** `200 OK` (`IReadOnlyList<PaymentResponse>`).

### 2.4. `POST /api/v1/payments/simulate` (Simulação Direta)
- **Objetivo:** Endpoint síncrono para testes pontuais de simulação bancária sem necessidade de publicar mensagens no RabbitMQ.
- **Payload:** `OrderPlacedIntegrationEvent` (`OrderId`, `UserId`, `GameId`, `Price`).
- **Respostas:** `201 Created` (`PaymentResponse`) ou `400 Bad Request`.

### 2.5. `GET /health` (HealthCheck)
- Retorna status de saúde para o Docker Compose: `{ "status": "Health", "service": "PaymentAPI" }`.

---

## 3. Documentação Interativa com Scalar

Em ambiente de desenvolvimento, a API disponibiliza o **Scalar**:

👉 **URL de Acesso:** `http://localhost:8083/scalar/v1`

Com tema moderno (*Moon*), permitindo testar diretamente todos os endpoints e inspecionar os esquemas JSON de resposta.

---

## 4. Pipeline no `Program.cs`

Localização: `src/FCG02.PaymentsAPI.API/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Injeção de Dependências
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline HTTP e Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FIAP Cloud Games - PaymentsAPI");
        options.WithTheme(ScalarTheme.Moon);
    });
}

app.UseHttpsRedirection();

// Endpoints
app.MapPaymentEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "Health", service = "PaymentAPI", timestamp = DateTime.UtcNow }));

app.Run();
```

---

## 5. Navegação

- ⬅️ **[Voltar para a Camada de Infraestrutura (`infrastructure.md`)](infrastructure.md)**
- ➡️ **[Avançar para a Suíte de Testes Unitários (`tests.md`)](tests.md)**
