## Relevant Files

- `src/Rcv.Core/Domain/Option.cs` – Immutable record representing a poll option.
- `src/Rcv.Core/Domain/RankedBallot.cs` – Immutable ballot model holding a voter's ordered choices.
- `src/Rcv.Core/Domain/RcvResult.cs` – Immutable aggregate result returned by the tally.
- `src/Rcv.Core/Domain/RoundSummary.cs` – Immutable per-round snapshot of vote counts and eliminations.
- `src/Rcv.Core/RankedChoicePoll.cs` – Public façade exposing the purely functional API.
- `src/Rcv.Core/IRcvCalculator.cs` – Strategy interface for voting calculators.
- `src/Rcv.Core/Calculators/InstantRunoffCalculator.cs` – Instant-runoff implementation.
- `tests/Rcv.Core.Tests/RankedChoicePollTests.cs` – Tests for the public facade and validation.
- `tests/Rcv.Core.Tests/RcvAlgorithmTests.cs` – Algorithm scenario tests.
- `tests/Rcv.Core.Tests/DomainModelTests.cs` – Domain-model validation tests.
- `src/Rcv.Core/Rcv.Core.csproj` – Library project file including NuGet metadata.
- `README.md` – Library overview and quick-start guide.

### Notes

- **Current status (2026-08-08)**: Phase 1 implementation remains complete with 29 passing tests and 95.34% line coverage. Package metadata and packing are configured, but `Rcv.Core` has not been published to NuGet and the repository has no release tag.
- Tests use xUnit; run with `dotnet test`.
- Each domain model produces XML doc comments for IntelliSense and NuGet.
- **Purely functional design**: `RankedChoicePoll.CalculateResult()` takes ballots as input and returns results. No mutable state.
- **Immutability**: All models use read-only properties with constructor validation.
- **Thread safety**: Library is thread-safe after construction due to immutable design.
- Maintain SOLID design by keeping calculation logic in `InstantRunoffCalculator` and exposing only immutable models through the public API.

## Tasks

- [x] 1.0 Define the public API and data structures for the RCV NuGet package
  - [x] 1.1 Review PRD API specification and map required classes & methods
  - [x] 1.2 Create immutable domain models (`Option`, `RankedBallot`, `RcvResult`, `RoundSummary`) with XML comments, read-only properties, and constructor validation; ensure all models support `System.Text.Json` serialization
  - [x] 1.3 Draft `RankedChoicePoll` façade with constructor and `CalculateResult()` accepting calculator via dependency injection (Strategy pattern for pluggable algorithms)
  - [x] 1.4 Validate naming, accessibility, and immutability against C# conventions; ensure nullable reference types are enabled
  - [x] 1.5 Write initial unit tests asserting API shape (compiles, correct signatures); validate minimum 2 options required and options have unique IDs

- [x] 2.0 Implement the core ranked-choice tallying algorithm
  - [x] 2.1 Add `IRcvCalculator` interface and `InstantRunoffCalculator` implementation with single-winner elimination loop; handle immediate majority winner (>50% first-choice votes); support partial ballots (voters ranking subset of options); random tie-breaking for tied eliminations
  - [x] 2.2 Pass minimal "happy-path" unit tests (simple three-option election)
  - [x] 2.3 Refactor for readability (extract helper methods, apply LINQ where clear)

- [x] 3.0 Handle edge cases and input validation
  - [x] 3.1 Validate ballots (duplicate rankings in RankedBallot constructor); throw descriptive exceptions for invalid ballots
  - [x] 3.2 Detect and report ties; expose `IsTie` and `TiedOptions` in `RcvResult`
  - [x] 3.3 Guard against empty ballot sets and throw descriptive exceptions
  - [x] 3.4 Extend tests to cover invalid ballots, complete tie, tie-for-last-place, and immediate majority scenarios

- [x] 4.0 Produce comprehensive statistics and round summaries
  - [x] 4.1 Populate `RoundSummary` during each elimination round
  - [x] 4.2 Aggregate per-option statistics (vote counts per round, final totals)
  - [x] 4.3 Document statistics fields in README with usage examples

- [x] 5.0 Write tests, documentation, and prepare the package for NuGet publishing
  - [x] 5.1 Achieve ≥90% code coverage with xUnit and edge-case fixtures (currently 95.34% line coverage)
  - [x] 5.2 Generate XML documentation file and include in `.csproj` for NuGet
  - [x] 5.3 Add README sections: installation, basic sample, contribution guidelines
  - [x] 5.4 Configure `dotnet pack` target with versioning and license metadata
  - [x] 5.5 Set up GitHub Actions workflow: restore → build → test → pack

## Implementation Notes

- **Strategy Pattern**: Implemented `IRcvCalculator` interface to allow pluggable voting algorithms. `InstantRunoffCalculator` is the IRV implementation.
- **Random Tie-Breaking**: When multiple candidates tie for last place, one is randomly eliminated. Random instance passed as parameter for deterministic testing.
- **Test Coverage**: 29 tests passing, covering domain models, API validation, and algorithm scenarios (immediate majority, elimination rounds, ties, partial ballots, edge cases, unknown option ID validation).
- **Historical Code Coverage**: Phase 1 recorded 95.39% line coverage and 98.14% branch coverage. Coverage is not currently enforced by CI.
- **Validation**: All ballots validated to ensure option IDs exist in poll options (fail-fast principle).
- **Documentation**: README includes Quick Start, statistics analysis examples, and comprehensive API reference.

## Completed Tasks

All implementation and package-preparation tasks in the PRD v1 NuGet module are complete:
- ✅ Domain models with immutability and validation
- ✅ Instant-runoff voting calculator with Strategy pattern
- ✅ Comprehensive test coverage (29 tests, 95%+ coverage)
- ✅ Round-by-round statistics and tracking
- ✅ README documentation with examples
- ✅ NuGet package configuration and local packing
- ✅ GitHub Actions build, test, and package-artifact pipeline

Publishing the package and creating a tagged release remain release-management work outside the completed implementation checklist.
