# Stage 1: construção e publicação dos artefatos (SDK Completo)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto .csproj preservando a estrutura de pastas
# permite aproveitar o cache do Docker para o restore de pacotes NuGet
COPY src/FCG02.PaymentsAPI.Domain/FCG02.PaymentsAPI.Domain.csproj src/FCG02.PaymentsAPI.Domain/
COPY src/FCG02.PaymentsAPI.Application/FCG02.PaymentsAPI.Application.csproj src/FCG02.PaymentsAPI.Application/
COPY src/FCG02.PaymentsAPI.Infrastructure/FCG02.PaymentsAPI.Infrastructure.csproj src/FCG02.PaymentsAPI.Infrastructure/
COPY src/FCG02.PaymentsAPI.API/FCG02.PaymentsAPI.API.csproj src/FCG02.PaymentsAPI.API/

# Restaura todas as dependências da solução através do projeto de entrada (API)
RUN dotnet restore src/FCG02.PaymentsAPI.API/FCG02.PaymentsAPI.API.csproj

# Copia todo o código-fonte restante
COPY src/ src/

# Compila e publica os binários em modo release otimizados para produção
RUN dotnet publish src/FCG02.PaymentsAPI.API/FCG02.PaymentsAPI.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 2: gera a imagem final de runtime (mínima e segura)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ativa o usuário não root nativo do .NET para segurança
USER app

# expõe a porta não privilegiada padrão do .net 10
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# copia apenas os binários compilados gerados no stage 1
COPY --from=build /app/publish .

# define o ponto de entrada da aplicação
ENTRYPOINT [ "dotnet", "FCG02.PaymentsAPI.API.dll" ]