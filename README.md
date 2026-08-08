# Ranked Choice Voting Platform

A .NET ranked choice voting platform under development. The repository contains a reusable instant-runoff voting library, an ASP.NET Core API, and a React frontend scaffold.

## Current Status

Last updated: 2026-08-07

| Area | Status |
|---|---|
| `Rcv.Core` voting library | Complete and tested |
| Authentication API | Google and Microsoft OAuth implemented; manual provider testing remains |
| Poll management API | Implemented except the documented status list filter |
| Voting API | Implemented; automatic enforcement of `ClosesAt` remains |
| Results API and `Rcv.Core` integration | Not implemented |
| React web application | Vite starter only |
| Deployment and Slack/Teams integrations | Not implemented |

See [`tasks/tasks-phase2-web-app.md`](tasks/tasks-phase2-web-app.md) for the authoritative implementation tracker. The PRD and Phase 2 plan describe the intended product and target architecture, not completed functionality.

## Repository Structure

```text
src/Rcv.Core/          Reusable instant-runoff voting library
src/Rcv.Web.Api/       ASP.NET Core API
src/rcv-web-ui/        React and TypeScript frontend scaffold
tests/                 Core and API tests
tasks/                 Product plans and implementation trackers
```

## Rcv.Core Features

- **Fair Elections**: Implements instant-runoff voting (IRV) with proper ranked choice tallying
- **Comprehensive Statistics**: Round-by-round elimination data, vote transfer tracking, and participation metrics
- **Edge Case Handling**: Robust tie detection, partial ballot support, and input validation
- **Developer Friendly**: Clean, immutable API with comprehensive XML documentation
- **High Performance**: Efficient algorithms designed for concurrent usage

## Using Rcv.Core

`Rcv.Core` is configured for packaging as version 1.0.0 but is not currently published to NuGet. Projects in this repository use a project reference. To produce a local package:

```bash
dotnet pack src/Rcv.Core/Rcv.Core.csproj -c Release
```

## Quick Start

```csharp
using Rcv.Core;
using Rcv.Core.Calculators;
using Rcv.Core.Domain;

// Define your poll options
var alice = new Option(Guid.NewGuid(), "Alice");
var bob = new Option(Guid.NewGuid(), "Bob");
var charlie = new Option(Guid.NewGuid(), "Charlie");

var poll = new RankedChoicePoll(new[] { alice, bob, charlie });

// Create ballots (voter preferences)
var ballots = new[]
{
    new RankedBallot(new[] { alice.Id, bob.Id, charlie.Id }),
    new RankedBallot(new[] { bob.Id, alice.Id }),
    new RankedBallot(new[] { charlie.Id, alice.Id, bob.Id })
};

// Calculate results using instant-runoff voting
var calculator = new InstantRunoffCalculator();
var result = poll.CalculateResult(ballots, calculator);

Console.WriteLine($"Winner: {result.Winner?.Label ?? "No winner (tie)"}");
Console.WriteLine($"Total rounds: {result.Rounds.Count}");
```

## Analyzing Results

The `RcvResult` object provides comprehensive round-by-round statistics:

```csharp
var result = poll.CalculateResult(ballots, calculator);

// Check for winner or tie
if (result.IsTie)
{
    Console.WriteLine("Election resulted in a tie:");
    foreach (var option in result.TiedOptions)
    {
        Console.WriteLine($"  - {option.Label}");
    }
}
else
{
    Console.WriteLine($"Winner: {result.Winner!.Label}");
}

// Analyze round-by-round elimination
foreach (var round in result.Rounds)
{
    Console.WriteLine($"\nRound {round.RoundNumber}:");

    // Show vote distribution for this round
    foreach (var kvp in round.VoteCounts)
    {
        var option = options.First(o => o.Id == kvp.Key);
        Console.WriteLine($"  {option.Label}: {kvp.Value} votes");
    }

    // Show who was eliminated (if anyone)
    if (round.EliminatedOption != null)
    {
        Console.WriteLine($"  Eliminated: {round.EliminatedOption.Label}");
    }
}

// View final vote totals
Console.WriteLine("\nFinal vote totals:");
foreach (var kvp in result.FinalVoteTotals)
{
    var option = options.First(o => o.Id == kvp.Key);
    Console.WriteLine($"  {option.Label}: {kvp.Value} votes");
}
```

## Core API

### Main Components

- **`RankedChoicePoll`**: Facade for conducting elections with pluggable calculator algorithms
- **`IRcvCalculator`**: Interface for implementing different RCV algorithms (e.g., instant-runoff, Borda count)
- **`InstantRunoffCalculator`**: Instant-runoff voting (IRV) implementation with random tie-breaking

### Domain Models (Immutable)

- **`Option`**: Record representing a poll option/candidate
- **`RankedBallot`**: Voter's ranked preferences (validates no duplicate rankings)
- **`RcvResult`**: Complete election results with winner/tie status, rounds, and final vote totals
- **`RoundSummary`**: Single round's vote distribution and eliminated candidate

## Development

### Building

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

The frontend is currently a buildable Vite scaffold:

```bash
cd src/rcv-web-ui
npm ci
npm run build
```

### Creating NuGet Package

```bash
dotnet pack -c Release
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.