# BioStack Mission Control Backend

A local-first C# ASP.NET Core backend for biometrics and protocol observability. Built with .NET 8 LTS, Entity Framework Core 8, and SQLite.

## Architecture

The solution follows a clean layered architecture with clear separation of concerns:

```
src/
├── BioStack.Domain/          # Core domain models, enums, value objects
├── BioStack.Contracts/       # DTOs for requests and responses
├── BioStack.Infrastructure/  # EF Core, repositories, knowledge base
├── BioStack.Application/     # Business logic services
└── BioStack.Api/             # API endpoints, configuration, hosting

tests/
├── BioStack.Domain.Tests/    # Domain value object tests
├── BioStack.Application.Tests/ # Calculator and overlap engine tests
└── BioStack.Api.Tests/       # Integration tests
```

## Domain Entities

- **PersonProfile**: User profile with biometric data (sex, weight, goals, notes)
- **CompoundRecord**: Tracked compounds/supplements with status and dates
- **CheckIn**: Daily health metrics (weight, sleep, energy, mood, GI symptoms)
- **ProtocolPhase**: Named protocol phases with start/end dates
- **TimelineEvent**: Unified event log aggregating all profile changes
- **KnowledgeEntry**: Educational compound information with pathways
- **InteractionFlag**: Compound overlap detection results

## API Endpoints

### Profiles
- `GET /api/v1/profiles` - List all profiles
- `POST /api/v1/profiles` - Create profile
- `GET /api/v1/profiles/{id}` - Get profile details
- `PUT /api/v1/profiles/{id}` - Update profile

### Compounds
- `GET /api/v1/profiles/{profileId}/compounds` - List compounds for profile
- `POST /api/v1/profiles/{profileId}/compounds` - Create compound
- `PUT /api/v1/compounds/{id}` - Update compound
- `DELETE /api/v1/compounds/{id}` - Delete compound

### Check-Ins
- `GET /api/v1/profiles/{profileId}/checkins` - List check-ins
- `POST /api/v1/profiles/{profileId}/checkins` - Create check-in

### Protocol Phases
- `GET /api/v1/profiles/{profileId}/phases` - List phases
- `POST /api/v1/profiles/{profileId}/phases` - Create phase

### Timeline
- `GET /api/v1/profiles/{profileId}/timeline` - Get unified timeline

### Calculators
- `POST /api/v1/calculators/reconstitution` - Reconstitution calculation
- `POST /api/v1/calculators/volume` - Volume calculation
- `POST /api/v1/calculators/conversion` - Unit conversion

### Knowledge Base
- `GET /api/v1/knowledge/compounds` - List all compounds
- `GET /api/v1/knowledge/compounds/{name}` - Get compound details
- `POST /api/v1/knowledge/overlap-check` - Check compound overlaps

### Health
- `GET /health` - Health check

## Key Features

### Calculator Service
Provides three mathematical calculators with built-in safety disclaimers:

1. **Reconstitution**: Calculate concentration (mcg/unit) from peptide amount and diluent volume
   - Formula: `Concentration = (Peptide mg * 1000) / (Diluent mL * 100)`

2. **Volume**: Calculate injection volume from desired dose and concentration
   - Formula: `Volume = Desired Dose / Concentration`

3. **Conversion**: Convert between units with provided conversion factors
   - Formula: `Converted = Amount * ConversionFactor`

All calculator outputs include: `"This is a mathematical calculation only. Not medical advice."`

### Knowledge Base
Seeded with educational compound information:
- BPC-157 (peptide, tissue-repair, gi-protective, angiogenesis)
- TB-500 (peptide, tissue-repair, anti-inflammatory, cell-migration)
- MOTS-C (peptide, metabolic-regulation, insulin-sensitivity, exercise-mimetic)
- NAD+ (coenzyme, cellular-energy, dna-repair, sirtuin-activation)
- Retatrutide (pharmaceutical, glucose-regulation, appetite-regulation, metabolic)

### Overlap Detection
The overlap service identifies when two or more compounds share pathway tags:
- Compares all compound pairs
- Returns flags for shared pathways
- Includes confidence levels and disclaimers

### Timeline Aggregation
Unified event log that captures:
- Compound start/end dates
- Check-in creation
- Protocol phase changes
- Goal updates
- Notes and status changes

## Database

SQLite local database (`biostack.db`). Schema is automatically created on first run via `EnsureCreated()`.

**Key Tables:**
- PersonProfiles
- CompoundRecords
- CheckIns
- ProtocolPhases
- TimelineEvents
- InteractionFlags

## Testing

### Domain Tests
- Calculator value object validation
- Disclaimer handling

### Application Tests
- Calculator math correctness (reconstitution, volume, conversion)
- Overlap engine pathway matching logic
- Input validation and edge cases

### Integration Tests
- Full CRUD lifecycle for profiles
- Endpoint response codes and schemas
- Data persistence

Run tests:
```bash
dotnet test
```

## Building and Running

### Prerequisites
- .NET 8 SDK
- No external dependencies required (SQLite included)

### Restore and Build
```bash
dotnet restore
dotnet build
```

### Run API Server
```bash
cd src/BioStack.Api
dotnet run
```

Server starts on `https://localhost:5001` and `http://localhost:5000`.

### Swagger Documentation
Visit `http://localhost:5000/swagger` for interactive API documentation.

### CORS Policy
Configured to allow requests from:
- `http://localhost:3000`
- `http://localhost:3001`
- `http://localhost:3043`

## Code Organization

**Services** implement business logic with CancellationToken support on all async methods.

**Repositories** provide data access with generic base class and specialized methods.

**Endpoints** use ASP.NET Core minimal APIs organized into logical groups.

**DTOs** are immutable records with clear separation between requests/responses.

**Entities** use shadow properties for EF Core relationships with proper navigation.

## Safety & Disclaimers

All calculator outputs include disclaimers stating they are "mathematical calculations only. Not medical advice."

The knowledge base is for educational reference only with explicit disclaimers on compound entries.

Overlap detection confidence levels indicate "Educational reference only" for all flags.

## Development Notes

- Nullable reference types enabled throughout
- CancellationToken on all async methods
- Dependency injection for all services
- No service locator antipatterns
- Business logic in Application/Domain, not API controllers
- Immutable records for DTOs where appropriate
