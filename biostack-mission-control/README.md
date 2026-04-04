# BioStack Mission Control

Local-first biometrics and protocol observability platform. Track compounds, log check-ins, correlate timelines, and run safe math — not medical advice.

## Quick Start

### Docker (recommended)

```bash
docker compose up --build
```

- UI: http://localhost:3000
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

### Manual

#### Backend

```bash
cd backend
dotnet restore
dotnet ef database update --project src/BioStack.Infrastructure --startup-project src/BioStack.Api
dotnet run --project src/BioStack.Api
```

API starts on http://localhost:5000

#### Frontend

```bash
cd frontend
npm install
npm run dev
```

UI starts on http://localhost:3000

## Architecture

```
/backend
  /src
    BioStack.Api            → HTTP endpoints, Swagger, health checks
    BioStack.Application    → Use cases, services, validators
    BioStack.Domain         → Entities, value objects, enums
    BioStack.Infrastructure → EF Core SQLite, repositories, knowledge adapters
    BioStack.Contracts      → Request/response DTOs
  /tests
    BioStack.Api.Tests
    BioStack.Application.Tests
    BioStack.Domain.Tests

/frontend
  Next.js 16 App Router + TypeScript + Tailwind + shadcn/ui + Recharts
```

## API Surface (v1)

| Resource | Endpoints |
|---|---|
| Profiles | GET/POST /api/v1/profiles, GET/PUT /api/v1/profiles/{id} |
| Compounds | GET/POST /api/v1/profiles/{id}/compounds, PUT/DELETE /api/v1/compounds/{id} |
| Check-Ins | GET/POST /api/v1/profiles/{id}/checkins |
| Phases | GET/POST /api/v1/profiles/{id}/phases |
| Timeline | GET /api/v1/profiles/{id}/timeline |
| Calculators | POST /api/v1/calculators/reconstitution, volume, conversion |
| Knowledge | GET /api/v1/knowledge/compounds, GET /api/v1/knowledge/compounds/{name}, POST /api/v1/knowledge/overlap-check |
| Health | GET /health |

## Seeded Knowledge

BPC-157, TB-500, MOTS-C, NAD+, Retatrutide — educational metadata with pathway tags for overlap detection.

## Safety Boundary

This system does not provide dosing recommendations, schedules, injection instructions, medical advice, or clinical decisioning. Calculator outputs are mathematical calculations only. Knowledge content is educational reference material with evidence tiering and source citations.

## Running Tests

```bash
cd backend
dotnet test
```

## Stack

- .NET 8 LTS / ASP.NET Core / EF Core / SQLite
- Next.js 16 / TypeScript / Tailwind CSS / shadcn/ui / Recharts
- Docker Compose for local orchestration

## Known Issues (v1)

- No authentication (placeholder for future)
- Knowledge engine uses local seed data only (adapter interface ready for PubMed, FDA, ClinicalTrials.gov)
- SQLite for portability — repository interfaces ready for Postgres swap
- No real-time updates (polling or manual refresh)
