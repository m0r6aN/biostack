FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BioStack.sln .
COPY src/BioStack.Domain/BioStack.Domain.csproj src/BioStack.Domain/
COPY src/BioStack.Contracts/BioStack.Contracts.csproj src/BioStack.Contracts/
COPY src/BioStack.Application/BioStack.Application.csproj src/BioStack.Application/
COPY src/BioStack.Infrastructure/BioStack.Infrastructure.csproj src/BioStack.Infrastructure/
COPY src/BioStack.Api/BioStack.Api.csproj src/BioStack.Api/
COPY src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj src/BioStack.KnowledgeWorker/
COPY src/BioStack.Cognition/BioStack.Cognition.csproj src/BioStack.Cognition/
COPY src/BioStack.Cognition.CollectiveAdapter/BioStack.Cognition.CollectiveAdapter.csproj src/BioStack.Cognition.CollectiveAdapter/
COPY tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj tests/BioStack.Api.Tests/
COPY tests/BioStack.Domain.Tests/BioStack.Domain.Tests.csproj tests/BioStack.Domain.Tests/
COPY tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj tests/BioStack.Application.Tests/
COPY tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj tests/BioStack.KnowledgeWorker.Tests/
RUN dotnet restore BioStack.sln

COPY . .
RUN dotnet publish src/BioStack.Api/BioStack.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "BioStack.Api.dll"]
