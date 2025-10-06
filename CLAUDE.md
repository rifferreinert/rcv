# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET ranked choice voting (RCV) platform consisting of:
- **Rcv.Core**: A NuGet library implementing instant-runoff voting (IRV) algorithm with round-by-round elimination and statistics
- **Future components**: Web app, Slack/Teams integrations (see tasks/prd-v1.md for full roadmap)

The core library is designed to be reusable, stateless, and completely independent of UI, storage, or user management concerns.

## Development Commands

### Build
```bash
dotnet build                                    # Build entire solution
dotnet build -c Release                         # Release build
```

### Testing
```bash
dotnet test                                     # Run all tests
dotnet test --verbosity detailed                # Verbose test output
dotnet test --filter "FullyQualifiedName~RcvCalculatorEdgeCaseTests"  # Run specific test class
```

### NuGet Package
```bash
dotnet pack -c Release                          # Create NuGet package
dotnet pack -c Release --output ./artifacts     # Output to specific directory
```

## Architecture

### Solution Structure
```
src/Rcv.Core/          # Core RCV library (NuGet package)
tests/Rcv.Core.Tests/  # xUnit tests
```

### Core Library Design

**Public API** (`RankedChoicePoll`):
- Facade exposing poll creation, ballot addition, and result calculation
- Immutable domain models: `Option`, `RankedBallot`, `RcvResult`, `RoundSummary`
- All models support `System.Text.Json` serialization

**Internal Implementation** (`RcvCalculator`):
- Instant-runoff voting algorithm with round-by-round elimination
- Handles partial ballots, immediate majority winners, tie detection
- Maintains SOLID principles: calculation logic isolated from public API

**Key Principles**:
- No database, session, or UI logic in the library
- Option ID and ballot ID management is external (consuming app's responsibility)
- Throw descriptive exceptions for invalid input (unknown option IDs, duplicate rankings)
- Comprehensive XML documentation for IntelliSense

### Test Organization
- `RankedChoicePollTests.cs`: Public API and core logic tests
- `RcvCalculatorEdgeCaseTests.cs`: Edge cases and regression tests
- Target: ≥90% code coverage

## Code Style & Practices

### C# Conventions
- Use records for immutable data models
- XML doc comments on all public members
- Validate all inputs; throw exceptions if invalid
- Follow SOLID principles (single responsibility per class)
- Prefer clear, concise variable names over abbreviations

### Test-Driven Development (TDD)
When implementing features:
1. Write tests first (with user feedback before implementation)
2. Write minimum code to pass tests
3. Refactor for readability

**Important**: After writing tests, wait for user confirmation before implementing. The user is learning C# (coming from Python background), so explain concepts as you go and summarize what was done and why after each feature.

### Visual Studio Tasks
XML files typically modified through Visual Studio UI (`.csproj`, `.sln`) should be edited in Visual Studio when possible. Instruct the user to open Visual Studio for these tasks, then verify completion.

## CI/CD

GitHub Actions workflow (`.github/workflows/ci.yml`):
1. Restore dependencies
2. Build (Release configuration)
3. Run tests
4. Pack NuGet package
5. Upload artifacts

Triggers: pushes to `main`/`develop`, PRs to `main`

## Current Development Phase

**Phase 1: RCV NuGet Module** (see tasks/tasks-prd-v1-nuget.md)
- Focus on core algorithm implementation
- Comprehensive edge case handling and validation
- Full test coverage and documentation
- NuGet package preparation

Next phases will add web app and platform integrations.
