# RCV NuGet Package API Implementation Plan

## Overview
This plan outlines the implementation of the public API and data structures for the Ranked Choice Voting (RCV) NuGet package based on the PRD API specification.

## Core Components

### 1. Domain Models (Immutable Records)

#### Option.cs
- **Purpose**: Represents a voting option/candidate
- **Properties**:
  - `Guid Id` - Unique identifier for the option
  - `string Label` - Display name/description
- **Design**: Immutable record with value equality
- **Location**: `src/Rcv.Core/Domain/Option.cs`

#### RankedBallot.cs
- **Purpose**: Represents a voter's ordered preferences
- **Properties**:
  - `IReadOnlyList<Guid> RankedOptionIds` - Ordered list of option IDs (most preferred first)
- **Design**: Immutable with validation in constructor
- **Location**: `src/Rcv.Core/Domain/RankedBallot.cs`

#### RcvResult.cs
- **Purpose**: Comprehensive result of the RCV tallying process
- **Properties**:
  - `Option? Winner` - The winning option (null if tie with no resolution)
  - `IReadOnlyList<RoundSummary> Rounds` - Round-by-round elimination details
  - `bool IsTie` - Indicates if election ended in a tie
  - `IReadOnlyList<Option> TiedOptions` - Options involved in a tie
  - `IReadOnlyDictionary<Option, int> FinalVoteTotals` - Final vote counts per option
- **Design**: Immutable result object
- **Location**: `src/Rcv.Core/Domain/RcvResult.cs`

#### RoundSummary.cs
- **Purpose**: Snapshot of a single elimination round
- **Properties**:
  - `int RoundNumber` - Sequential round identifier (1-based)
  - `IReadOnlyDictionary<Option, int> VoteCounts` - Vote distribution for this round
  - `Option? EliminatedOption` - Option eliminated this round (null if final round)
- **Design**: Immutable record
- **Location**: `src/Rcv.Core/Domain/RoundSummary.cs`

### 2. Public API Facade

#### RankedChoicePoll.cs
- **Purpose**: Main entry point for the library
- **Constructor**: 
  - `RankedChoicePoll(IEnumerable<Option> options)`
  - Validates options are non-empty and have unique IDs
- **Methods**:
  - `void AddBallots(IEnumerable<RankedBallot> ballots)` - Accumulates ballots for tallying
  - `RcvResult CalculateResult()` - Executes RCV algorithm and returns results
- **Design**: Stateful facade that delegates to internal calculator
- **Location**: `src/Rcv.Core/RankedChoicePoll.cs`

### 3. Internal Implementation

#### RcvCalculator.cs
- **Purpose**: Core RCV algorithm implementation (internal)
- **Responsibilities**:
  - Instant runoff voting logic
  - Round-by-round elimination
  - Tie detection and handling
  - Statistics compilation
- **Design**: Stateless calculator, pure functions where possible
- **Location**: `src/Rcv.Core/Internal/RcvCalculator.cs`

## Implementation Sequence

### Phase 1: Domain Models (Task 1.2)
1. Create `Option` record with XML documentation
2. Create `RankedBallot` with validation logic
3. Create `RoundSummary` record
4. Create `RcvResult` with all statistics properties
5. Ensure all models are immutable with appropriate equality implementations

### Phase 2: Public API Facade (Task 1.3)
1. Create `RankedChoicePoll` class structure
2. Implement constructor with option validation
3. Add `AddBallots` method (store internally, no processing yet)
4. Add `CalculateResult` method stub (returns mock data initially)
5. Add comprehensive XML documentation for IntelliSense

### Phase 3: Validation & Conventions (Task 1.4)
1. Review naming conventions (PascalCase for public members)
2. Ensure proper accessibility modifiers (public API, internal implementation)
3. Validate immutability patterns
4. Add guard clauses and argument validation
5. Implement proper exception types with clear messages

### Phase 4: Initial Tests (Task 1.5)
1. Test domain model construction and validation
2. Test API method signatures and return types
3. Test immutability guarantees
4. Test edge cases for constructors (null, empty, duplicates)
5. Verify XML documentation generates correctly

## Key Design Decisions

### Immutability
- All domain models use immutable patterns (records or readonly properties)
- Collections exposed as `IReadOnlyList` or `IReadOnlyDictionary`
- No setters on public properties

### Validation Strategy
- Constructor validation for domain models
- Method argument validation with descriptive exceptions
- Option ID uniqueness enforced at poll creation
- Ballot validation checks for unknown option IDs

### Error Handling
- `ArgumentNullException` for null inputs
- `ArgumentException` for invalid data (empty collections, duplicates)
- `InvalidOperationException` for incorrect API usage (e.g., calculating before adding ballots)
- Custom exceptions for domain-specific errors (e.g., `TieException` if needed)

### Testability
- Internal calculator separated from public facade
- Dependency injection ready (if needed in future)
- Pure functions in calculator for easy unit testing
- Mock-friendly interfaces where appropriate

## XML Documentation Standards

Each public member should include:
- `<summary>` - Brief description
- `<param>` - For each parameter
- `<returns>` - For non-void methods
- `<exception>` - For each exception that can be thrown
- `<example>` - For complex usage scenarios

Example:
```csharp
/// <summary>
/// Calculates the ranked choice voting result based on accumulated ballots.
/// </summary>
/// <returns>The complete RCV result including winner and round summaries.</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when no ballots have been added.
/// </exception>
public RcvResult CalculateResult()
```

## Success Criteria

- [ ] All domain models compile with no warnings
- [ ] Public API matches PRD specification
- [ ] XML documentation builds without errors
- [ ] Basic unit tests pass (API shape validation)
- [ ] Code follows C# conventions and SOLID principles
- [ ] Immutability is maintained throughout
- [ ] Clear separation between public API and internal implementation
