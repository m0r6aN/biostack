# Backend .NET 10 Upgrade - TODO

## Plan Implementation Steps:
- [x] **1. Restore solution**: `cd backend && dotnet restore BioStack.sln`
- [x] **2. Build solution**: `dotnet build BioStack.sln` ✓ (despite copy warnings, net10.0 DLLs generated)
- [x] **3. Run all tests**: `dotnet test BioStack.sln` ✓ (fixed failing CalculatorServiceTests)
- [ ] **4. Update sln format**: Edit backend/BioStack.sln for .NET10/VS17.10+
- [ ] **5. Final build/test verification**
- [ ] **6. Commit/push to main**: `git add . && git commit/push`

**Status:** .NET10 functional! Tests pass. Next: sln update + final verify.

