## Relevant Files

- `src/Rc- [ ] 2.0 Implement the core ranked-choice tallying algorithm
  - [ ] 2.1 Add `RcvCalculator` with single-winner elimination loop; handle immediate majority winner (>50% first-choice votes); support partial ballots (voters ranking subset of options)
  - [ ] 2.2 Pass minimal "happy-path" unit tests (simple three-option election)
  - [ ] 2.3 Refactor for readability (extract helper methods, apply LINQ where clear)re/RankedChoicePoll.cs` – Public façade exposing the main API surface.
- `src/Rcv.Core/Domain/Option.cs` – Immutable record representing a poll option.
- `src/Rcv.Core/Domain/RankedBallot.cs` – Ballot model holding a voter’s ordered choices.
- `src/Rcv.Core/Domain/RcvResult.cs` – Aggregate result returned by the tally.
- `src/Rcv.Core/Domain/RoundSummary.cs` – Per-round snapshot of vote counts and eliminations.
- `src/Rcv.Core/Internal/RcvCalculator.cs` – Internal algorithm implementation.
- `tests/Rcv.Core.Tests/RankedChoicePollTests.cs` – Unit tests for public API & core logic.
- `tests/Rcv.Core.Tests/RcvCalculatorEdgeCaseTests.cs` – Edge-case & regression tests.
- `src/Rcv.Core/Rcv.Core.csproj` – Library project file including NuGet metadata.
- `README.md` – Library overview and quick-start guide.

### Notes

- Tests use xUnit; run with `dotnet test`.
- Each domain model produces XML doc comments for IntelliSense and NuGet.
- Maintain SOLID design by keeping calculation logic in `RcvCalculator` and exposing only immutable models through the public API.

## Tasks

- [ ] 1.0 Define the public API and data structures for the RCV NuGet package
  - [ ] 1.1 Review PRD API sketch and map required classes & methods
  - [ ] 1.2 Create domain models (`Option`, `RankedBallot`, `RcvResult`, `RoundSummary`) with XML comments; ensure all models support `System.Text.Json` serialization
  - [ ] 1.3 Draft `RankedChoicePoll` façade with constructor, `AddBallots`, and `CalculateResult` stubs
  - [ ] 1.4 Validate naming, accessibility, and immutability against C# conventions
  - [ ] 1.5 Write initial unit tests asserting API shape (compiles, correct signatures); validate minimum 2 options required and options have unique IDs

- [ ] 2.0 Implement the core ranked-choice tallying algorithm
  - [ ] 2.1 Add `RcvCalculator` with single-winner elimination loop (no edge-case handling yet)
  - [ ] 2.2 Pass minimal “happy-path” unit tests (simple three-option election)
  - [ ] 2.3 Refactor for readability (extract helper methods, apply LINQ where clear)

- [ ] 3.0 Handle edge cases and input validation
  - [ ] 3.1 Validate ballots (unknown option IDs, duplicate rankings); throw descriptive exceptions for invalid ballots on save attempt
  - [ ] 3.2 Detect and report ties; expose `IsTie` and `TiedOptions` in `RcvResult`
  - [ ] 3.3 Guard against empty ballot sets and throw descriptive exceptions
  - [ ] 3.4 Extend tests to cover invalid ballots, complete tie, and immediate majority scenarios

- [ ] 4.0 Produce comprehensive statistics and round summaries
  - [ ] 4.1 Populate `RoundSummary` during each elimination round
  - [ ] 4.2 Aggregate per-option statistics (first-choice votes, final totals)
  - [ ] 4.3 Document statistics fields in README with usage examples

- [ ] 5.0 Write tests, documentation, and prepare the package for NuGet publishing
  - [ ] 5.1 Achieve ≥90 % code coverage with xUnit and edge-case fixtures
  - [ ] 5.2 Generate XML documentation file and include in `.csproj` for NuGet
  - [ ] 5.3 Add README sections: installation, basic sample, contribution guidelines
  - [ ] 5.4 Configure `dotnet pack` target with versioning and license metadata
  - [ ] 5.5 Set up GitHub Actions workflow: restore → build → test → pack
