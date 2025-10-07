# Rcv.Core - Ranked Choice Voting Library

A comprehensive .NET library for conducting ranked choice voting (RCV) elections with transparent round-by-round elimination tracking and detailed statistics.

## Features

- **Fair Elections**: Implements instant-runoff voting (IRV) with proper ranked choice tallying
- **Comprehensive Statistics**: Round-by-round elimination data, vote transfer tracking, and participation metrics
- **Edge Case Handling**: Robust tie detection, partial ballot support, and input validation
- **Developer Friendly**: Clean, immutable API with comprehensive XML documentation
- **High Performance**: Efficient algorithms designed for concurrent usage

## Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package Rcv.Core
```

Or via Package Manager Console in Visual Studio:

```powershell
Install-Package Rcv.Core
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
dotnet build
```

### Running Tests

```bash
dotnet test
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