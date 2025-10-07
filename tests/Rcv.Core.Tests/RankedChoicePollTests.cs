using Rcv.Core.Calculators;
using Rcv.Core.Domain;

namespace Rcv.Core.Tests;

/// <summary>
/// Tests for RankedChoicePoll public API
/// </summary>
public class RankedChoicePollTests
{
    [Fact]
    public void Constructor_CreatesSuccessfully()
    {
        // Arrange
        var options = new[]
        {
            new Option(Guid.NewGuid(), "Alice"),
            new Option(Guid.NewGuid(), "Bob")
        };

        // Act
        var poll = new RankedChoicePoll(options);

        // Assert
        Assert.NotNull(poll);
    }

    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RankedChoicePoll(null!));
    }

    [Fact]
    public void Constructor_ThrowsOnFewerThanTwoOptions()
    {
        // Arrange
        var singleOption = new[] { new Option(Guid.NewGuid(), "Alice") };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new RankedChoicePoll(singleOption));
        Assert.Contains("at least 2", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyOptions()
    {
        // Arrange
        var emptyOptions = Array.Empty<Option>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RankedChoicePoll(emptyOptions));
    }

    [Fact]
    public void Constructor_ThrowsOnDuplicateOptionIds()
    {
        // Arrange
        var duplicateId = Guid.NewGuid();
        var options = new[]
        {
            new Option(duplicateId, "Alice"),
            new Option(duplicateId, "Bob")  // Same ID, different label
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new RankedChoicePoll(options));
        Assert.Contains("unique", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculateResult_ThrowsOnNullBallots()
    {
        // Arrange
        var options = new[]
        {
            new Option(Guid.NewGuid(), "Alice"),
            new Option(Guid.NewGuid(), "Bob")
        };
        var poll = new RankedChoicePoll(options);
        var calculator = new InstantRunoffCalculator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => poll.CalculateResult(null!, calculator));
    }

    [Fact]
    public void CalculateResult_ThrowsOnNullCalculator()
    {
        // Arrange
        var options = new[]
        {
            new Option(Guid.NewGuid(), "Alice"),
            new Option(Guid.NewGuid(), "Bob")
        };
        var poll = new RankedChoicePoll(options);
        var ballots = new[] { new RankedBallot(new[] { options[0].Id }) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => poll.CalculateResult(ballots, null!));
    }

    [Fact]
    public void CalculateResult_ThrowsOnBallotWithUnknownOptionId()
    {
        // Arrange
        var alice = new Option(Guid.NewGuid(), "Alice");
        var bob = new Option(Guid.NewGuid(), "Bob");
        var poll = new RankedChoicePoll(new[] { alice, bob });

        var unknownOptionId = Guid.NewGuid(); // Not in poll options
        var ballots = new[]
        {
            new RankedBallot(new[] { alice.Id, unknownOptionId }) // Contains unknown ID
        };

        var calculator = new InstantRunoffCalculator();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => poll.CalculateResult(ballots, calculator));
        Assert.Contains("unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
