# syntax=docker/dockerfile:1
#
# Multi-stage build da API .NET. O estágio de build carrega o SDK completo; a imagem
# final leva apenas o runtime ASP.NET + os binários publicados — sem SDK, sem código
# fonte, sem cache de NuGet sobrando.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copia só os arquivos de projeto antes do restore: enquanto as dependências não
# mudarem, o Docker reaproveita a camada de restore mesmo que o código mude.
COPY InvoiSys.slnx ./
COPY src/InvoiSys.Domain/InvoiSys.Domain.csproj src/InvoiSys.Domain/
COPY src/InvoiSys.Application/InvoiSys.Application.csproj src/InvoiSys.Application/
COPY src/InvoiSys.Infrastructure/InvoiSys.Infrastructure.csproj src/InvoiSys.Infrastructure/
COPY src/InvoiSys.Api/InvoiSys.Api.csproj src/InvoiSys.Api/
RUN dotnet restore src/InvoiSys.Api/InvoiSys.Api.csproj

COPY src/ src/
RUN dotnet publish src/InvoiSys.Api/InvoiSys.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# A imagem oficial do runtime ASP.NET já traz o usuário sem privilégios "app" —
# criar outro aqui falharia, e a API não tem motivo para rodar como root.

WORKDIR /app

COPY --from=build /app/publish ./

# Os prompts do pipeline de IA são carregados em runtime pelo PromptLoader, que sobe
# a partir do diretório da aplicação procurando a pasta "prompts/" — por isso eles
# precisam existir na imagem, versionados como conteúdo, não embutidos no código.
COPY prompts ./prompts

# ASPNETCORE_HTTP_PORTS define a porta de escuta, e o modo --healthcheck lê a mesma
# variável — mudar a porta aqui mantém o health check correto sozinho.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=1

USER app

EXPOSE 8080

# A imagem aspnet não traz curl nem wget. Em vez de instalar um pacote na imagem
# final só para o health check, a própria aplicação responde ao argumento
# --healthcheck consultando /health e saindo com o código apropriado.
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD ["dotnet", "InvoiSys.Api.dll", "--healthcheck"]

CMD ["dotnet", "InvoiSys.Api.dll"]
