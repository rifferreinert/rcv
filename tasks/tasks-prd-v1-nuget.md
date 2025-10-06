## Relevant Files

- `src/Rcv.Core/Domain/Option.cs` – Immutable record representing a poll option.
- `src/Rcv.Core/Domain/RankedBallot.cs` – Immutable ballot model holding a voter's ordered choices.
- `src/Rcv.Core/Domain/RcvResult.cs` – Immutable aggregate result returned by the tally.
- `src/Rcv.Core/Domain/RoundSummary.cs` – Immutable per-round snapshot of vote counts and eliminations.
- `src/Rcv.Core/RankedChoicePoll.cs` – Public façade exposing the purely functional API.
- `src/Rcv.Core/Internal/RcvCalculator.cs` – Internal algorithm implementation.
- `tests/Rcv.Core.Tests/RankedChoicePollTests.cs` – Unit tests for public API & core logic.
- `tests/Rcv.Core.Tests/RcvCalculatorEdgeCaseTests.cs` – Edge-case & regression tests.
- `src/Rcv.Core/Rcv.Core.csproj` – Library project file including NuGet metadata.
- `README.md` – Library overview and quick-start guide.

### Notes

- Tests use xUnit; run with `dotnet test`.
- Each domain model produces XML doc comments for IntelliSense and NuGet.
- **Purely functional design**: `RankedChoicePoll.CalculateResult()` takes ballots as input and returns results. No mutable state.
- **Immutability**: All models use read-only properties with constructor validation.
- **Thread safety**: Library is thread-safe after construction due to immutable design.
- Maintain SOLID design by keeping calculation logic in `RcvCalculator` and exposing only immutable models through the public API.

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
  - [ ] 4.3 Document statistics fields in README with usage examples

- [x] 5.0 Write tests, documentation, and prepare the package for NuGet publishing
  - [ ] 5.1 Achieve ≥90% code coverage with xUnit and edge-case fixtures (need to verify coverage)
  - [x] 5.2 Generate XML documentation file and include in `.csproj` for NuGet
  - [x] 5.3 Add README sections: installation, basic sample, contribution guidelines
  - [x] 5.4 Configure `dotnet pack` target with versioning and license metadata
  - [x] 5.5 Set up GitHub Actions workflow: restore → build → test → pack

## Implementation Notes

- **Strategy Pattern**: Implemented `IRcvCalculator` interface to allow pluggable voting algorithms. `InstantRunoffCalculator` is the IRV implementation.
- **Random Tie-Breaking**: When multiple candidates tie for last place, one is randomly eliminated. Random instance passed as parameter for deterministic testing.
- **Test Coverage**: 28 tests passing, covering domain models, API validation, and algorithm scenarios (immediate majority, elimination rounds, ties, partial ballots, edge cases).

## Remaining Tasks

- [ ] Verify ≥90% code coverage (run coverage analysis)
- [ ] Document statistics usage in README with examples
- [ ] Add validation for unknown option IDs in ballots (currently not validated)
