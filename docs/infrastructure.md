# PaymentsAPI - Documentação Técnica: Camada de Infraestrutura

A camada **`FCG02.PaymentsAPI.Infrastructure`** provê a persistência relacional com o **Azure Database** via **Entity Framework Core** e a mensageria orientada a eventos com **RabbitMQ 7.x**.

---

## 📑 Sumário

- [1. Visão Geral](#1-visão-geral)
- [2. Banco de Dados Relacional e EF Core (`PaymentsDbContext`)](#2-banco-de-dados-relacional-e-ef-core-paymentsdbcontext)
  - [2.1. Mapeamento Objeto-Relacional (Fluent API)](#21-mapeamento-objeto-relacional-fluent-api)
  - [2.2. Índices de Alta Performance e Idempotência](#22-índices-de-alta-performance-e-idempotência)
  - [2.3. Migrações e Atualizações](#23-migrações-e-atualizações)
- [3. Repositório Relacional: `PaymentRepository`](#3-repositório-relacional-paymentrepository)
- [4. Mensageria Assíncrona com RabbitMQ](#4-mensageria-assíncrona-com-rabbitmq)
  - [4.1. Publicador: `RabbitMqEventPublisher`](#41-publicador-rabbitmqeventpublisher)
  - [4.2. Consumidor: `OrderPlacedConsumerService`](#42-consumidor-orderplacedconsumerservice)
- [5. Resolução de Escopo em Serviços em Segundo Plano](#5-resolução-de-escopo-em-serviços-em-segundo-plano)
- [6. Navegação](#6-navegação)

---

## 1. Visão Geral

A infraestrutura encapsula toda a complexidade de comunicação com redes externas:
- Provedor relacional `Microsoft.EntityFrameworkCore.SqlServer` apontando para o Azure Database (`fcg-server.database.windows.net`, banco `fcg-02-paymentsdb`).
- Configuração de tolerância a falhas transitórias com `EnableRetryOnFailure()`.
- Nomes de tabelas e colunas em padrão `snake_case`.
- Consumo resiliente com `BasicAckAsync` e `BasicNackAsync`.

---

## 2. Banco de Dados Relacional e EF Core (`PaymentsDbContext`)

Localização: `src/FCG02.PaymentsAPI.Infrastructure/Data/`

### 2.1. Mapeamento Objeto-Relacional (Fluent API)
Na classe `PaymentTransactionConfiguration`:
- **Tabela:** `payment_transactions`
- **Chave Primária:** Coluna `id` (UUIDv7 gerado no domínio).
- **Precisão Monetária:** Coluna `amount` com `HasPrecision(18, 2)`.
- **Enums como Texto:** `method` e `status` mapeados com `.HasConversion<string>()` para máxima legibilidade em auditorias SQL manuais.

### 2.2. Índices de Alta Performance e Idempotência
1. **`ix_payment_transactions_order_id` (ÚNICO):**
   ```csharp
   builder.HasIndex(t => t.OrderId).IsUnique();
   ```
   Garante que o banco de dados rejeite fisicamente qualquer tentativa de cobrar duas vezes o mesmo pedido (`OrderId`), blindando o sistema contra entregas duplicadas de mensagens do broker.
2. **`ix_payment_transactions_user_id`:** Otimiza consultas do extrato de transações de um jogador.
3. **`ix_payment_transactions_status`:** Agiliza relatórios operacionais por status (`Approved`, `Rejected`, `Pending`).

### 2.3. Migrações e Atualizações
As migrações do EF Core estão versionadas na pasta `Data/Migrations/`. A migração inicial `20261028124012_InitialCreate.cs` cria a tabela e todos os índices estruturados.

---

## 3. Repositório Relacional: `PaymentRepository`

Localização: `src/FCG02.PaymentsAPI.Infrastructure/Repositories/PaymentRepository.cs`

- Utiliza o `PaymentsDbContext` para operações assíncronas.
- As consultas de auditoria utilizam `.AsNoTracking()` para minimizar alocações de memória.
- Operações de mutação usam `SaveChangesAsync()` garantindo commit atômico na transação do Azure Database.

---

## 4. Mensageria Assíncrona com RabbitMQ

Localização: `src/FCG02.PaymentsAPI.Infrastructure/Messaging/`

### 4.1. Publicador: `RabbitMqEventPublisher`
- Declara a Exchange Topic `fcg.payments.events` de forma idempotente.
- Publica o evento `PaymentProcessedIntegrationEvent` com `persistent: true`.
- Roteia com a chave `payment.approved` ou `payment.rejected`.

### 4.2. Consumidor: `OrderPlacedConsumerService`
Opera como um `BackgroundService`:
- Declara a Topic Exchange `fcg.catalog.events`.
- Declara a Fila durável `payments.order-placed` e estabelece a ligação com a routing key `order.placed`.
- Aplica `BasicQosAsync(prefetchCount: 1)` para distribuição justa entre réplicas.
- Ao receber o pedido, delega o processamento financeiro para o `IPaymentService` e emite o `BasicAckAsync`.

---

## 5. Resolução de Escopo em Serviços em Segundo Plano

Como o `OrderPlacedConsumerService` é um `BackgroundService` registrado como **Singleton**, ele **não pode injetar diretamente o `PaymentsDbContext` ou o `IPaymentService`** (que são Scoped):

```csharp
consumer.ReceivedAsync += async (model, ea) =>
{
    // Cria um escopo efêmero para a mensagem recebida
    using var scope = _serviceScopeFactory.CreateScope();
    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

    await paymentService.ProcessOrderAsync(order);

    // Confirmação manual de remoção da fila
    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
};
```

---

## 6. Navegação

- ⬅️ **[Voltar para a Camada de Aplicação (`application.md`)](application.md)**
- ➡️ **[Avançar para a Camada de Apresentação (`api.md`)](api.md)**
