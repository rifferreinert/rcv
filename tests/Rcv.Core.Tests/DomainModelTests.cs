using Rcv.Core.Domain;

namespace Rcv.Core.Tests;

/// <summary>
/// Tests for domain models (Option, RankedBallot, RoundSummary, RcvResult)
/// </summary>
public class DomainModelTests
{
    #region Option Tests

    [Fact]
    public void Option_CreatesSuccessfully()
    {
        // Arrange
        var id = Guid.NewGuid();
        var label = "Alice";

        // Act
        var option = new Option(id, label);

        // Assert
        Assert.Equal(id, option.Id);
        Assert.Equal(label, option.Label);
    }

    [Fact]
    public void Option_SupportsValueEquality()
    {
        // Arrange
        var id = Guid.NewGuid();
        var option1 = new Option(id, "Alice");
        var option2 = new Option(id, "Alice");

        // Act & Assert
        Assert.Equal(option1, option2);
        Assert.True(option1 == option2);
    }

    #endregion

    #region RankedBallot Tests

    [Fact]
    public void RankedBallot_CreatesSuccessfully()
    {
        // Arrange
        var options = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var ballot = new RankedBallot(options);

        // Assert
        Assert.Equal(3, ballot.RankedOptionIds.Count);
        Assert.Equal(options[0], ballot.RankedOptionIds[0]);
    }

    [Fact]
    public void RankedBallot_ThrowsOnNullInput()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RankedBallot(null!));
    }

    [Fact]
    public void RankedBallot_ThrowsOnDuplicateIds()
    {
        // Arrange
        var duplicateId = Guid.NewGuid();
        var options = new[] { duplicateId, Guid.NewGuid(), duplicateId };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new RankedBallot(options));
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RankedBallot_IsImmutable()
    {
        // Arrange
        var options = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var ballot = new RankedBallot(options);

        // Act - modify source list
        options.Add(Guid.NewGuid());

        // Assert - ballot unchanged
        Assert.Equal(2, ballot.RankedOptionIds.Count);
    }

    #endregion

    #region RoundSummary Tests

    [Fact]
    public void RoundSummary_CreatesSuccessfully()
    {
        // Arrange
        var voteCounts = new Dictionary<Guid, int>
        {
            { Guid.NewGuid(), 10 },
            { Guid.NewGuid(), 5 }
        }.AsReadOnly();
        var eliminated = new Option(Guid.NewGuid(), "Bob");

        // Act
        var summary = new RoundSummary(1, voteCounts, eliminated);

        // Assert
        Assert.Equal(1, summary.RoundNumber);
        Assert.Equal(2, summary.VoteCounts.Count);
        Assert.Equal(eliminated, summary.EliminatedOption);
    }

    [Fact]
    public void RoundSummary_AllowsNullEliminatedOption()
    {
        // Arrange
        var voteCounts = new Dictionary<Guid, int> { { Guid.NewGuid(), 10 } }.AsReadOnly();

        // Act
        var summary = new RoundSummary(1, voteCounts, null);

        // Assert
        Assert.Null(summary.EliminatedOption);
    }

    [Fact]
    public void RoundSummary_ThrowsOnInvalidRoundNumber()
    {
        // Arrange
        var voteCounts = new Dictionary<Guid, int> { { Guid.NewGuid(), 10 } }.AsReadOnly();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RoundSummary(0, voteCounts));
        Assert.Throws<ArgumentException>(() => new RoundSummary(-1, voteCounts));
    }

    [Fact]
    public void RoundSummary_ThrowsOnNullVoteCounts()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoundSummary(1, null!));
    }

    #endregion

    #region RcvResult Tests

    [Fact]
    public void RcvResult_CreatesSuccessfullyWithWinner()
    {
        // Arrange
        var winner = new Option(Guid.NewGuid(), "Alice");
        var rounds = new[]
        {
            new RoundSummary(1, new Dictionary<Guid, int> { { winner.Id, 100 } }.AsReadOnly())
        };
        var finalTotals = new Dictionary<Guid, int> { { winner.Id, 100 } }.AsReadOnly();

        // Act
        var result = new RcvResult(winner, rounds, finalTotals);

        // Assert
        Assert.Equal(winner, result.Winner);
        Assert.False(result.IsTie);
        Assert.Empty(result.TiedOptions);
        Assert.Single(result.Rounds);
    }

    [Fact]
    public void RcvResult_CreatesSuccessfullyWithTie()
    {
        // Arrange
        var option1 = new Option(Guid.NewGuid(), "Alice");
        var option2 = new Option(Guid.NewGuid(), "Bob");
        var rounds = new[]
        {
            new RoundSummary(1, new Dictionary<Guid, int>
            {
                { option1.Id, 50 },
                { option2.Id, 50 }
            }.AsReadOnly())
        };
        var finalTotals = new Dictionary<Guid, int>
        {
            { option1.Id, 50 },
            { option2.Id, 50 }
        }.AsReadOnly();
        var tiedOptions = new[] { option1, option2 };

        // Act
        var result = new RcvResult(null, rounds, finalTotals, tiedOptions);

        // Assert
        Assert.Null(result.Winner);
        Assert.True(result.IsTie);
        Assert.Equal(2, result.TiedOptions.Count);
    }

    [Fact]
    public void RcvResult_ThrowsWhenNoWinnerAndNoTie()
    {
        // Arrange
        var rounds = new[]
        {
            new RoundSummary(1, new Dictionary<Guid, int> { { Guid.NewGuid(), 100 } }.AsReadOnly())
        };
        var finalTotals = new Dictionary<Guid, int> { { Guid.NewGuid(), 100 } }.AsReadOnly();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new RcvResult(null, rounds, finalTotals));
        Assert.Contains("Winner is required", ex.Message);
    }

    [Fact]
    public void RcvResult_ThrowsOnEmptyRounds()
    {
        // Arrange
        var winner = new Option(Guid.NewGuid(), "Alice");
        var emptyRounds = Array.Empty<RoundSummary>();
        var finalTotals = new Dictionary<Guid, int> { { winner.Id, 100 } }.AsReadOnly();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new RcvResult(winner, emptyRounds, finalTotals));
        Assert.Contains("at least one round", ex.Message);
    }

    [Fact]
    public void RcvResult_ThrowsOnNullParameters()
    {
        // Arrange
        var winner = new Option(Guid.NewGuid(), "Alice");
        var rounds = new[]
        {
            new RoundSummary(1, new Dictionary<Guid, int> { { winner.Id, 100 } }.AsReadOnly())
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RcvResult(winner, null!, new Dictionary<Guid, int>().AsReadOnly()));
        Assert.Throws<ArgumentNullException>(() => new RcvResult(winner, rounds, null!));
    }

    #endregion
}
