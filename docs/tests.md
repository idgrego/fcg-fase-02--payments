# PaymentsAPI - Documentação Técnica: Suíte de Testes Unitários

A **PaymentsAPI** conta com uma suíte rigorosa de testes automatizados construída com **xUnit**, **Moq** e **FluentAssertions**, assegurando o comportamento da máquina de estados financeiros e dos fluxos assíncronos de aplicação.

---

## 📑 Sumário

- [1. Estratégia de Testes](#1-estratégia-de-testes)
- [2. Testes de Domínio (`PaymentTransactionTests`)](#2-testes-de-domínio-paymenttransactiontests)
  - [2.1. Invariantes de Criação](#21-invariantes-de-criação)
  - [2.2. Transições de Estado](#22-transições-de-estado)
  - [2.3. Bloqueio de Transições Inválidas](#23-bloqueio-de-transições-inválidas)
- [3. Testes de Aplicação (`PaymentServiceTests`)](#3-testes-de-aplicação-paymentservicetests)
  - [3.1. Isolamento com Moq](#31-isolamento-com-moq)
  - [3.2. Cenários de Sucesso, Recusa e Publicação](#32-cenários-de-sucesso-recusa-e-publicação)
- [4. Execução dos Testes via CLI](#4-execução-dos-testes-via-cli)
- [5. Navegação](#5-navegação)

---

## 1. Estratégia de Testes

Os testes seguem o padrão **AAA (Arrange, Act, Assert)**:
- **Domínio Puro:** Sem necessidade de banco de dados ou broker em execução. As regras da entidade são validadas em milissegundos.
- **Camada de Aplicação:** Dublês de teste para `IPaymentRepository`, `IPaymentGatewaySimulator` e `IEventPublisher`.

---

## 2. Testes de Domínio (`PaymentTransactionTests`)

Localização: `tests/FCG02.PaymentsAPI.UnitTests/Domain/PaymentTransactionTests.cs`

### 2.1. Invariantes de Criação
- Valida que instanciar com dados válidos atribui `Status = Pending`, gera UUIDv7 sequencial e registra `CreatedAt`.
- Rejeita `OrderId`, `UserId` ou `GameId` vazios (`Guid.Empty`).
- Rejeita valores negativos (`Amount < 0`).

### 2.2. Transições de Estado
- `Approve(note)`: Transita de `Pending` para `Approved`, carimba `ProcessedAt` e salva a nota de autorização.
- `Reject(reason)`: Transita de `Pending` para `Rejected`, carimba `ProcessedAt` e exige motivo não-vazio.

### 2.3. Bloqueio de Transições Inválidas
- Tentar aprovar uma transação já aprovada lança `InvalidOperationException`.
- Tentar aprovar uma transação já rejeitada lança `InvalidOperationException`.
- Tentar rejeitar uma transação já aprovada lança `InvalidOperationException`.

---

## 3. Testes de Aplicação (`PaymentServiceTests`)

Localização: `tests/FCG02.PaymentsAPI.UnitTests/Application/PaymentServiceTests.cs`

### 3.1. Isolamento com Moq
- Simula o repositório (`Mock<IPaymentRepository>`), verificando se `AddAsync` e `UpdateAsync` são invocados com os parâmetros exatos.
- Simula o gateway (`Mock<IPaymentGatewaySimulator>`), permitindo injetar resultados de aprovação ou rejeição controlados.
- Simula o publicador (`Mock<IEventPublisher>`), verificando se o evento `PaymentProcessedIntegrationEvent` foi despachado para a Exchange correta (`fcg.payments.events`) com a Routing Key adequada (`payment.approved` ou `payment.rejected`).

---

## 4. Execução dos Testes via CLI

No terminal, na pasta raiz ou em `fcg-fase-02--payments`:

```powershell
dotnet test tests/FCG02.PaymentsAPI.UnitTests/FCG02.PaymentsAPI.UnitTests.csproj --logger "console;verbosity=detailed"
```

Todos os **20 testes unitários** devem ser executados com sucesso total (0 falhas).

---

## 5. Navegação

- ⬅️ **[Voltar para a Apresentação (`api.md`)](api.md)**
- ➡️ **[Avançar para a Containerização (`docker.md`)](docker.md)**
