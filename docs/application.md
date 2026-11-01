# PaymentsAPI - Documentação Técnica: Camada de Aplicação

A camada **`FCG02.PaymentsAPI.Application`** orquestra o processamento financeiro de pedidos de compra, aplicando regras determinísticas de simulação de adquirente bancária, registrando transações de auditoria e publicando os eventos de resultado para o ecossistema **FIAP Cloud Games**.

---

## 📑 Sumário

- [1. Estrutura e Responsabilidades](#1-estrutura-e-responsabilidades)
- [2. O Simulador de Gateway (`PaymentGatewaySimulator`)](#2-o-simulador-de-gateway-paymentgatewaysimulator)
  - [2.1. Regras Determinísticas de Aprovação e Recusa](#21-regras-determinísticas-de-aprovação-e-recusa)
- [3. O Serviço de Pagamentos (`PaymentService`)](#3-o-serviço-de-pagamentos-paymentservice)
- [4. DTOs (Data Transfer Objects)](#4-dtos-data-transfer-objects)
- [5. Contratos de Eventos de Integração](#5-contratos-de-eventos-de-integração)
  - [5.1. `OrderPlacedIntegrationEvent` (Consumido)](#51-orderplacedintegrationevent-consumido)
  - [5.2. `PaymentProcessedIntegrationEvent` (Publicado)](#52-paymentprocessedintegrationevent-publicado)
- [6. Navegação](#6-navegação)

---

## 1. Estrutura e Responsabilidades

A camada de aplicação não acessa o banco de dados diretamente; ela depende da interface `IPaymentRepository` e de `IEventPublisher`:
- Recebe o pedido de pagamento originado pela `CatalogAPI`.
- Instancia a entidade `PaymentTransaction` com status `Pending`.
- Aciona o simulador bancário para obter a decisão de autorização.
- Atualiza a entidade para `Approved` ou `Rejected`.
- Persiste a auditoria no Azure Database.
- Publica o evento de integração para os microsserviços interessados.

---

## 2. O Simulador de Gateway (`PaymentGatewaySimulator`)

Localização: `src/FCG02.PaymentsAPI.Application/Services/PaymentGatewaySimulator.cs`

O simulador emula uma adquirente bancária real (como Cielo, Stone ou Stripe) com regras de risco determinísticas projetadas para testes acadêmicos e integrados:

### 2.1. Regras Determinísticas de Aprovação e Recusa

| Condição / Valor | Decisão | Código / Motivo Retornado |
|---|---|---|
| **`Amount == 0`** | **Aprovado** | `FREE-XXXXXXXX` (Jogos gratuitos liberados imediatamente) |
| **`Amount >= 5.000,00`** | **Recusado** | *"Transação bloqueada pelas políticas de risco antifraude (limite de R$ 5.000,00 excedido)."* |
| **`Amount == 999,99`** | **Recusado (Teste)** | *"Saldo ou limite de crédito insuficiente no cartão informado."* |
| **`Amount == 888,88`** | **Recusado (Teste)** | *"Cartão expirado ou temporariamente bloqueado pelo emissor."* |
| **Demais valores válidos** | **Aprovado** | `AUTH-XXXXXXXX` (Autorização concedida com sucesso) |

---

## 3. O Serviço de Pagamentos (`PaymentService`)

Localização: `src/FCG02.PaymentsAPI.Application/Services/PaymentService.cs`

- `ProcessOrderAsync`: Ponto de entrada invocado pelo consumidor RabbitMQ:
  1. Cria e salva a transação no estado `Pending`.
  2. Executa a análise no `IPaymentGatewaySimulator`.
  3. Invoca `transaction.Approve()` ou `transaction.Reject()`.
  4. Atualiza o banco via `_repository.UpdateAsync()`.
  5. Publica `PaymentProcessedIntegrationEvent` na exchange `fcg.payments.events` com a routing key correspondente (`payment.approved` ou `payment.rejected`).
- `GetByOrderIdAsync` & `GetByUserIdAsync`: Consultas de auditoria para suporte à API HTTP.

---

## 4. DTOs (Data Transfer Objects)

Localização: `src/FCG02.PaymentsAPI.Application/DTOs/`

- **`PaymentResponse`:** Representação pública completa de uma transação (`PaymentId`, `OrderId`, `UserId`, `GameId`, `Amount`, `Status`, `Reason`, `ProcessedAt`).
- **`ProcessPaymentDto`:** Dados de entrada para o endpoint de simulação direta via HTTP.
- **`PaymentStatusDto`:** Resumo compacto de status.

---

## 5. Contratos de Eventos de Integração

### 5.1. `OrderPlacedIntegrationEvent` (Consumido)
Recebido da fila `payments.order-placed`:
```csharp
public record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    DateTime CreatedAt
);
```

### 5.2. `PaymentProcessedIntegrationEvent` (Publicado)
Publicado na Topic Exchange `fcg.payments.events`:
```csharp
public record PaymentProcessedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Amount,
    string Status,
    string? Reason,
    DateTime ProcessedAt
);
```

---

## 6. Navegação

- ⬅️ **[Voltar para a Camada de Domínio (`domain.md`)](domain.md)**
- ➡️ **[Avançar para a Camada de Infraestrutura (`infrastructure.md`)](infrastructure.md)**
