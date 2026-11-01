# PaymentsAPI - Documentação Técnica: Orquestração com Kubernetes (K8s)

Este documento detalha a arquitetura de orquestração em nuvem da **PaymentsAPI** (`fcg-fase-02--payments`) em clusters **Kubernetes (K8s)**, atendendo aos padrões de governança, banco relacional isolado (Azure Database) e mensageria assíncrona da Fase 02.

---

## 📑 Sumário

- [1. Visão Geral da Topologia no Cluster](#1-visão-geral-da-topologia-no-cluster)
- [2. Estrutura de Manifestos (`/k8s`)](#2-estrutura-de-manifestos-k8s)
- [3. Segregação de Configurações: ConfigMap vs Secret](#3-segregação-de-configurações-configmap-vs-secret)
  - [3.1. `ConfigMap` (Parâmetros de Mensageria)](#31-configmap-parâmetros-de-mensageria)
  - [3.2. `Secret` (Credenciais em Base64)](#32-secret-credenciais-em-base64)
- [4. Especificação do `Deployment`](#4-especificação-do-deployment)
  - [4.1. Réplicas e Alta Disponibilidade](#41-réplicas-e-alta-disponibilidade)
  - [4.2. Injeção de Variáveis via `envFrom`](#42-injeção-de-variáveis-via-envfrom)
  - [4.3. Sondas de Integridade (Readiness e Liveness Probes)](#43-sondas-de-integridade-readiness-e-liveness-probes)
  - [4.4. Limites e Requisições de Recursos (Resources)](#44-limites-e-requisições-de-recursos-resources)
- [5. Exposição de Rede com `Service` (ClusterIP)](#5-exposição-de-rede-com-service-clusterip)
- [6. Comandos Essenciais de Operação (`kubectl`)](#6-comandos-essenciais-de-operação-kubectl)
- [7. Navegação](#7-navegação)

---

## 1. Visão Geral da Topologia no Cluster

A **PaymentsAPI** opera como o simulador de gateway financeiro e auditoria transacional do ecossistema:

```mermaid
graph TD
    Client["🌐 Consultas HTTP / Clientes"] -->|http://payments-api-service:80| Svc["🌐 Service: payments-api-service<br/>(ClusterIP - Porta 80)"]

    subgraph PodsGroup ["Pods Gerenciados pelo Deployment (Replicas: 2)"]
        Svc -->|Round-Robin| Pod1["📦 Pod: payments-api-xxxx-1<br/>API :8080 | Consumer Ativo"]
        Svc -->|Round-Robin| Pod2["📦 Pod: payments-api-xxxx-2<br/>API :8080 | Consumer Ativo"]
    end

    subgraph Configurations ["Injeção via envFrom"]
        CM["📋 ConfigMap: payments-api-config<br/>(RabbitMQ Host, Exchanges, Queues)"]
        Sec["🔐 Secret: payments-api-secret<br/>(Azure Database ConnString, Rabbit Pass)"]
    end

    CM -.-> Pod1
    CM -.-> Pod2
    Sec -.-> Pod1
    Sec -.-> Pod2
```

---

## 2. Estrutura de Manifestos (`/k8s`)

| Arquivo | Kind (Tipo) | Responsabilidade |
|---|---|---|
| **`01-configmap.yaml`** | `ConfigMap` | Exchanges e filas do RabbitMQ (`fcg.payments.events`, `payments.order-placed`) |
| **`02-secret.yaml`** | `Secret` | Connection string do Azure Database (`fcg-02-paymentsdb`) e senha do RabbitMQ em Base64 |
| **`03-deployment.yaml`** | `Deployment` | Réplicas (2), injeção `envFrom`, probes `/health` e limites de recursos |
| **`04-service.yaml`** | `Service` | Endpoint interno estável (ClusterIP) na porta 80 |

---

## 3. Segregação de Configurações: ConfigMap vs Secret

### 3.1. `ConfigMap` (Parâmetros de Mensageria)
- `ASPNETCORE_ENVIRONMENT`: `"Production"`
- `RabbitMQ__HostName`: `"rabbitmq-service"`
- `RabbitMQ__Port`: `"5672"`
- `RabbitMQ__UserName`: `"guest"`
- `RabbitMQ__Exchange`: `"fcg.payments.events"` (publicação do resultado financeiro)
- `RabbitMQ__CatalogExchange`: `"fcg.catalog.events"` (exchange do catálogo)
- `RabbitMQ__OrderPlacedQueue`: `"payments.order-placed"` (fila de pedidos)
- `RabbitMQ__OrderPlacedRoutingKey`: `"order.placed"`

### 3.2. `Secret` (Credenciais em Base64)
- `ConnectionStrings__DefaultConnection`: String de conexão com o Azure Database (banco `fcg-02-paymentsdb`) em Base64.
- `RabbitMQ__Password`: `guest` em Base64.

---

## 4. Especificação do `Deployment`

### 4.1. Réplicas e Alta Disponibilidade
Mantém `replicas: 2`. Ambas as réplicas mantêm o worker consumidor `OrderPlacedConsumerService` ativo, dividindo a carga de processamento de pedidos do RabbitMQ via *Competing Consumers*.

### 4.2. Injeção de Variáveis via `envFrom`
Injeta automaticamente todas as chaves do ConfigMap e Secret como variáveis de ambiente na aplicação .NET 10.

### 4.3. Sondas de Integridade (Readiness e Liveness Probes)
- **`readinessProbe`:** Verifica `GET /health` na porta 8080 a cada 10s (`initialDelaySeconds: 5`).
- **`livenessProbe`:** Verifica `GET /health` na porta 8080 a cada 20s (`initialDelaySeconds: 15`), reiniciando o Pod caso haja travamento no Kestrel.

### 4.4. Limites e Requisições de Recursos (Resources)
- **`requests`:** CPU `100m` e Memória `128Mi`.
- **`limits`:** CPU `300m` e Memória `256Mi`.

---

## 5. Exposição de Rede com `Service` (ClusterIP)

- **Tipo:** `ClusterIP`
- **Nome DNS Interno:** `payments-api-service`
- **Mapeamento:** Porta 80 externa do serviço direcionada para a porta 8080 interna do contêiner.

---

## 6. Comandos Essenciais de Operação (`kubectl`)

```powershell
# Validação prévia de sintaxe (dry-run)
kubectl apply -f k8s/ --dry-run=client

# Aplicar manifestos
kubectl apply -f k8s/

# Monitorar Pods
kubectl get pods -l app=payments-api

# Logs em tempo real
kubectl logs -f -l app=payments-api

# Teste local via túnel Port-Forward
kubectl port-forward svc/payments-api-service 8083:80
```

---

## 7. Navegação

- ⬅️ **[Voltar para a Containerização Docker (`docker.md`)](docker.md)**
- 📑 **[Voltar para o Índice da Documentação (`index.md`)](index.md)**
