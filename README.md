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

// Define your poll options
var options = new[]
{
    new Option(Guid.NewGuid(), "Alice"),
    new Option(Guid.NewGuid(), "Bob"),
    new Option(Guid.NewGuid(), "Charlie")
};

// Create a new poll
var poll = new RankedChoicePoll(options);

// Add ballots (voter preferences)
var ballots = new[]
{
    new RankedBallot { RankedOptionIds = new List<Guid> { alice.Id, bob.Id, charlie.Id } },
    new RankedBallot { RankedOptionIds = new List<Guid> { bob.Id, alice.Id } },
    new RankedBallot { RankedOptionIds = new List<Guid> { charlie.Id, alice.Id, bob.Id } }
};

poll.AddBallots(ballots);

// Calculate results
var result = poll.CalculateResult();

Console.WriteLine($"Winner: {result.Winner.Label}");
Console.WriteLine($"Total rounds: {result.Rounds.Count}");
```

## Core API

### Classes

- **`RankedChoicePoll`**: Main class for managing polls and calculating results
- **`Option`**: Immutable record representing a poll option
- **`RankedBallot`**: Represents a voter's ranked preferences
- **`RcvResult`**: Complete election results with statistics
- **`RoundSummary`**: Round-by-round elimination data

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