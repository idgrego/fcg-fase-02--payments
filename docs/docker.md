# PaymentsAPI - Documentação Técnica: Containerização com Docker

Este documento detalha o processo de empacotamento, segurança e orquestração em contêineres do microsserviço **PaymentsAPI** (`fcg-fase-02--payments`) em **.NET 10**.

---

## 📑 Sumário

- [1. Estratégia de Multi-Stage Build](#1-estratégia-de-multi-stage-build)
- [2. Segurança e Execução Não-Root (`USER app`)](#2-segurança-e-execução-não-root-user-app)
- [3. Otimização do Cache de Camadas (*Layer Caching*)](#3-otimização-do-cache-de-camadas-layer-caching)
- [4. Arquivo `.dockerignore`](#4-arquivo-dockerignore)
- [5. O `Dockerfile` Completo](#5-o-dockerfile-completo)
- [6. Integração ao Docker Compose & Banco Dedicado](#6-integração-ao-docker-compose--banco-dedicado)
- [7. Navegação](#7-navegação)

---

## 1. Estratégia de Multi-Stage Build

A PaymentsAPI utiliza **Multi-Stage Build** para manter os contêineres finais de produção extremamente leves e seguros:
- **Estágio de Build (`build`):** Imagem completa `mcr.microsoft.com/dotnet/sdk:10.0` para restore, compilação e publicação dos binários em modo Release.
- **Estágio de Runtime (`final`):** Imagem enxuta `mcr.microsoft.com/dotnet/aspnet:10.0` contendo apenas os binários de runtime do ASP.NET Core, reduzindo a imagem final para ~200 MB e minimizando vulnerabilidades.

---

## 2. Segurança e Execução Não-Root (`USER app`)

- O contêiner executa sob o usuário não-privilegiado padrão **`USER app`** (UID 1654).
- A aplicação escuta na porta interna **`8080`** (`ASPNETCORE_URLS=http://+:8080`), sem necessidade de privilégios de root.

---

## 3. Otimização do Cache de Camadas (*Layer Caching*)

No `Dockerfile`, todos os quatro arquivos `.csproj` da Clean Architecture são copiados isoladamente antes da execução do `dotnet restore`. Mudanças apenas em arquivos `.cs` reaproveitam o cache do NuGet e compilam a aplicação em poucos segundos.

---

## 4. Arquivo `.dockerignore`

Localização: `fcg-fase-02--payments/.dockerignore`

```dockerignore
**/.git
**/.vs
**/.vscode
**/bin
**/obj
**/*.user
**/*.suo
**/Dockerfile*
README.md
docs/
```

---

## 5. O `Dockerfile` Completo

Localização: `fcg-fase-02--payments/Dockerfile`

```dockerfile
# Stage 1: construção e publicação dos artefatos (SDK Completo)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto .csproj preservando a estrutura de pastas
COPY src/FCG02.PaymentsAPI.Domain/FCG02.PaymentsAPI.Domain.csproj src/FCG02.PaymentsAPI.Domain/
COPY src/FCG02.PaymentsAPI.Application/FCG02.PaymentsAPI.Application.csproj src/FCG02.PaymentsAPI.Application/
COPY src/FCG02.PaymentsAPI.Infrastructure/FCG02.PaymentsAPI.Infrastructure.csproj src/FCG02.PaymentsAPI.Infrastructure/
COPY src/FCG02.PaymentsAPI.API/FCG02.PaymentsAPI.API.csproj src/FCG02.PaymentsAPI.API/

# Restaura dependências externas a partir da API
RUN dotnet restore src/FCG02.PaymentsAPI.API/FCG02.PaymentsAPI.API.csproj

# Copia todo o código-fonte restante
COPY src/ src/

# Compila e publica os binários em modo Release
RUN dotnet publish src/FCG02.PaymentsAPI.API/FCG02.PaymentsAPI.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: imagem final de runtime (Mínima e Segura)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Ativa usuário não-root nativo do .NET 10 (UID 1654)
USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copia apenas os binários compilados gerados no stage 1
COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet", "FCG02.PaymentsAPI.API.dll" ]
```

---

## 6. Integração ao Docker Compose & Banco Dedicado

No arquivo `fcg-fase-02--orchestration/docker-compose.yml`:
- **Serviço (`payments-api`):**
  - Mapeado para a porta host **`8083:8080`**.
  - Conectado à rede virtual **`fcg-network`**.
  - Conexão com o banco dedicado `fcg-02-paymentsdb` no Azure Database (`fcg-server.database.windows.net`).
  - Dependência com `depends_on`:
    - `rabbitmq`: `condition: service_healthy`

---

## 7. Navegação

- ⬅️ **[Voltar para os Testes Unitários (`tests.md`)](tests.md)**
- 📑 **[Voltar para o Índice Central da Documentação (`index.md`)](index.md)**
