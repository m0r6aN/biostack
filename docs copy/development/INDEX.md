# BioStack Mission Control Backend - File Index

## Quick Navigation

### Getting Started
1. **QUICKSTART.md** - Build, run, and test guide
2. **README.md** - Architecture and feature overview
3. **PROJECT_SUMMARY.md** - Complete feature list
4. **IMPLEMENTATION_CHECKLIST.md** - Verification checklist

### Solution File
- **BioStack.sln** - Visual Studio solution with 8 projects

## Source Code Organization

### Domain Layer (`src/BioStack.Domain/`)
**Entities** - Core business models
- PersonProfile.cs
- CompoundRecord.cs
- CheckIn.cs
- ProtocolPhase.cs
- TimelineEvent.cs
- KnowledgeEntry.cs
- InteractionFlag.cs

**Enums** - Type definitions
- Sex.cs (Male, Female, Other, Unspecified)
- CompoundCategory.cs (Peptide, Coenzyme, Pharmaceutical, Supplement, Compound)
- CompoundStatus.cs (Planned, Active, Paused, Completed, Discontinued)
- SourceType.cs (Manual, ResearchPaper, KnowledgeBase, ImportedData)
- EventType.cs (CompoundStarted, CompoundEnded, CheckInCreated, ProtocolPhaseStarted, ...)
- EvidenceTier.cs (Limited, Moderate, Strong, Mechanistic)
- OverlapType.cs (PathwayOverlap, MechanismicSimilarity, PotentialInteraction, AdditiveBenefit)

**Value Objects** - Immutable value types
- CalculatorResult.cs

### Contracts Layer (`src/BioStack.Contracts/`)
**Requests** - Input DTOs
- CreateProfileRequest.cs
- UpdateProfileRequest.cs
- CreateCompoundRequest.cs
- UpdateCompoundRequest.cs
- CreateCheckInRequest.cs
- CreateProtocolPhaseRequest.cs
- ReconstitutionRequest.cs
- VolumeRequest.cs
- ConversionRequest.cs
- OverlapCheckRequest.cs

**Responses** - Output DTOs
- ProfileResponse.cs
- CompoundResponse.cs
- CheckInResponse.cs
- ProtocolPhaseResponse.cs
- TimelineEventResponse.cs
- CalculatorResultResponse.cs
- KnowledgeEntryResponse.cs
- InteractionFlagResponse.cs

### Application Layer (`src/BioStack.Application/Services/`)
**Service Implementations** - Business logic
- ProfileService.cs (CRUD for profiles)
- CompoundService.cs (CRUD for compounds)
- CheckInService.cs (Create and list check-ins)
- ProtocolPhaseService.cs (Create and list phases)
- TimelineService.cs (Aggregate timeline events)
- CalculatorService.cs (Reconstitution, Volume, Conversion math)
- KnowledgeService.cs (Query knowledge base)
- OverlapService.cs (Pathway intersection detection)

### Infrastructure Layer (`src/BioStack.Infrastructure/`)
**Persistence** - Database configuration
- Persistence/BioStackDbContext.cs (EF Core DbContext with all entities)

**Repositories** - Data access layer
- Repositories/IRepository.cs (Generic interface)
- Repositories/Repository.cs (Generic implementation)
- Repositories/PersonProfileRepository.cs (Profile-specific queries)
- Repositories/CompoundRecordRepository.cs (Compound-specific queries)
- Repositories/CheckInRepository.cs (Check-in-specific queries)
- Repositories/ProtocolPhaseRepository.cs (Phase-specific queries)
- Repositories/TimelineEventRepository.cs (Timeline-specific queries)
- Repositories/InteractionFlagRepository.cs (Flag storage)

**Knowledge Base** - Educational reference data
- Knowledge/IKnowledgeSource.cs (Interface)
- Knowledge/LocalKnowledgeSource.cs (Implementation with 5 seeded compounds)

### API Layer (`src/BioStack.Api/`)
**Configuration**
- Program.cs (DI setup, middleware, endpoint mapping)
- appsettings.json (SQLite connection string)
- appsettings.Development.json (Development logging)

**Endpoints** - HTTP API
- Endpoints/ProfileEndpoints.cs (4 profile endpoints)
- Endpoints/CompoundEndpoints.cs (4 compound endpoints)
- Endpoints/CheckInEndpoints.cs (2 check-in endpoints)
- Endpoints/ProtocolPhaseEndpoints.cs (2 phase endpoints)
- Endpoints/TimelineEndpoints.cs (1 timeline endpoint)
- Endpoints/CalculatorEndpoints.cs (3 calculator endpoints)
- Endpoints/KnowledgeEndpoints.cs (3 knowledge endpoints)

## Test Projects

### Domain Tests (`tests/BioStack.Domain.Tests/`)
- ValueObjects/CalculatorResultTests.cs (3 tests for value object)

### Application Tests (`tests/BioStack.Application.Tests/`)
- Services/CalculatorServiceTests.cs (9 tests for calculator math)
- Services/OverlapServiceTests.cs (3 tests for overlap detection)

### API Tests (`tests/BioStack.Api.Tests/`)
- Integration/ProfileEndpointsIntegrationTests.cs (5 integration tests for CRUD)

## Key Files by Responsibility

### If you need to...

**Add a new entity:**
1. Create entity in src/BioStack.Domain/Entities/
2. Add DbSet to BioStackDbContext
3. Configure in OnModelCreating
4. Create Repository interface and implementation
5. Register in Program.cs

**Add a new service:**
1. Create service interface in src/BioStack.Application/Services/
2. Implement service class with DI constructor
3. Register in Program.cs
4. Add tests in appropriate test project

**Add a new endpoint:**
1. Create endpoint method in existing or new Endpoints/XXXEndpoints.cs
2. Add MapXXX call to Program.cs
3. Test with integration tests

**Run the application:**
```bash
cd src/BioStack.Api
dotnet run
# Visit http://localhost:5000/swagger
```

**Run tests:**
```bash
dotnet test
```

## Project References

- BioStack.Domain (no dependencies)
- BioStack.Contracts → BioStack.Domain
- BioStack.Infrastructure → BioStack.Domain
- BioStack.Application → BioStack.Domain, BioStack.Infrastructure, BioStack.Contracts
- BioStack.Api → All projects
- Test projects → Respective source projects

## Configuration

**Database:** SQLite file-based (biostack.db)
**CORS:** Configured for localhost:3000, localhost:3001, and localhost:3043
**Swagger:** Available at /swagger endpoint
**Health Check:** Available at /health endpoint

## Statistics

- **Total Files:** 72 (C# + project files)
- **Lines of Code:** 2,005 (production) + 500+ (tests)
- **Entities:** 7
- **Services:** 8
- **Repositories:** 6
- **Endpoints:** 21
- **Tests:** 15+
- **Documentation:** 4 guide files

## Safety Features

All calculator outputs include: "This is a mathematical calculation only. Not medical advice."
All knowledge entries marked: "Educational reference only"
No directive language used: No "take", "inject", "administer", or "dose with"
