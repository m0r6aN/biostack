FROM mcr.microsoft.com/dotnet/sdk:10.0.100@sha256:c7445f141c04f1a6b454181bd098dcfa606c61ba0bd213d0a702489e5bd4cd71 AS build
WORKDIR /src

COPY backend/NuGet.Config backend/NuGet.Config
COPY backend/packages/ backend/packages/
COPY backend/src/BioStack.Domain/BioStack.Domain.csproj backend/src/BioStack.Domain/
COPY backend/src/BioStack.Contracts/BioStack.Contracts.csproj backend/src/BioStack.Contracts/
COPY backend/src/BioStack.Application/BioStack.Application.csproj backend/src/BioStack.Application/
COPY backend/src/BioStack.Infrastructure/BioStack.Infrastructure.csproj backend/src/BioStack.Infrastructure/
COPY backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj backend/src/BioStack.KnowledgeWorker/
RUN dotnet restore backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj \
    --configfile backend/NuGet.Config

COPY backend/src/ backend/src/
RUN dotnet publish backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0.0@sha256:d13bea17080a4fea1a7295a4fe29240123b1bf955a78ae08480d07bdf09496db AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY research/research-requests/market-interest-coverage-2026-07-24.v1.json /app/inputs/research-request.json
COPY research/source-authorization/recommended-seven-source-decisions.v1.json /app/inputs/source-decisions.json
COPY research/input/sources/pilot-source-registry.json /app/inputs/source-registry.json

RUN mkdir -p /app/ResearchOutput \
    && chown -R app:app /app

ENV DOTNET_ENVIRONMENT=Production
USER app
ENTRYPOINT ["dotnet", "BioStack.KnowledgeWorker.dll"]
