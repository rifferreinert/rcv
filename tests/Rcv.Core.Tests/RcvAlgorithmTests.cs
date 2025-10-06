using Rcv.Core.Calculators;
using Rcv.Core.Domain;

namespace Rcv.Core.Tests;

/// <summary>
/// Tests for the RCV algorithm implementation (happy path scenarios).
/// These tests define the expected behavior before implementation.
/// </summary>
public class RcvAlgorithmTests
{
    #region Immediate Majority Winner Tests

    [Fact]
    public void CalculateResult_ImmediateMajorityWinner_ReturnsWinnerInRound1()
    {
        // Arrange: Alice gets 60% of first-choice votes (immediate majority)
        var alice = new Option(Guid.NewGuid(), "Alice");
        var bob = new Option(Guid.NewGuid(), "Bob");
        var charlie = new Option(Guid.NewGuid(), "Charlie");

        var poll = new RankedChoicePoll(new[] { alice, bob, charlie });

        var ballots = new[]
        {
            // 6 ballots for Alice (60%)
            new RankedBallot(new[] { alice.Id, bob.Id, charlie.Id }),
            new RankedBallot(new[] { alice.Id, bob.Id, charlie.Id }),
            new RankedBallot(new[] { alice.Id, charlie.Id, bob.Id }),
            new RankedBallot(new[] { alice.Id, charlie.Id, bob.Id }),
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),

            // 2 ballots for Bob (20%)
            new RankedBallot(new[] { bob.Id, alice.Id }),
            new RankedBallot(new[] { bob.Id, charlie.Id }),

            // 2 ballots for Charlie (20%)
            new RankedBallot(new[] { charlie.Id, alice.Id }),
            new RankedBallot(new[] { charlie.Id, bob.Id })
        };

        // Act
        var calculator = new InstantRunoffCalculator();
        var result = poll.CalculateResult(ballots, calculator, new Random(42)); // Seeded for determinism

        // Assert
        Assert.NotNull(result);
        Assert.Equal(alice, result.Winner);
        Assert.False(result.IsTie);
        Assert.Empty(result.TiedOptions);
        Assert.Single(result.Rounds); // Should win in round 1 with >50%
        Assert.Equal(6, result.FinalVoteTotals[alice.Id]);
    }

    #endregion

    #region Elimination Required Tests

    [Fact]
    public void CalculateResult_NoImmediateMajority_EliminatesLowestAndRedistributes()
    {
        // Arrange: No one has majority, Charlie has fewest votes and is eliminated
        var alice = new Option(Guid.NewGuid(), "Alice");
        var bob = new Option(Guid.NewGuid(), "Bob");
        var charlie = new Option(Guid.NewGuid(), "Charlie");

        var poll = new RankedChoicePoll(new[] { alice, bob, charlie });

        var ballots = new[]
        {
            // Round 1: Alice=4, Bob=4, Charlie=2 (Charlie lowest, no tie)
            // After eliminating Charlie: Alice=5, Bob=5 → Tie!

            // 4 first-choice for Alice
            new RankedBallot(new[] { alice.Id, bob.Id }),
            new RankedBallot(new[] { alice.Id, bob.Id }),
            new RankedBallot(new[] { alice.Id, charlie.Id }),
            new RankedBallot(new[] { alice.Id }),

            // 4 first-choice for Bob
            new RankedBallot(new[] { bob.Id, charlie.Id }),
            new RankedBallot(new[] { bob.Id, alice.Id }),
            new RankedBallot(new[] { bob.Id }),
            new RankedBallot(new[] { bob.Id, alice.Id }),

            // 2 first-choice for Charlie (will be eliminated - clearly lowest)
            // One voter prefers Alice, one prefers Bob as 2nd choice
            new RankedBallot(new[] { charlie.Id, alice.Id }),
            new RankedBallot(new[] { charlie.Id, bob.Id })
        };

        // Act
        var calculator = new InstantRunoffCalculator();
        var result = poll.CalculateResult(ballots, calculator, new Random(42)); // Seeded for determinism

        // Assert
        Assert.True(result.IsTie); // Alice and Bob both end with 5 votes
        Assert.Null(result.Winner);
        Assert.Equal(2, result.TiedOptions.Count);
        Assert.Contains(alice, result.TiedOptions);
        Assert.Contains(bob, result.TiedOptions);

        Assert.Equal(2, result.Rounds.Count); // Round 1 + Round 2 after elimination

        // Round 1: Initial count
        var round1 = result.Rounds[0];
        Assert.Equal(1, round1.RoundNumber);
        Assert.Equal(4, round1.VoteCounts[alice.Id]);
        Assert.Equal(4, round1.VoteCounts[bob.Id]);
        Assert.Equal(2, round1.VoteCounts[charlie.Id]);
        Assert.Equal(charlie, round1.EliminatedOption); // Charlie clearly lowest

        // Round 2: After redistribution - results in tie
        var round2 = result.Rounds[1];
        Assert.Equal(2, round2.RoundNumber);
        Assert.Equal(5, round2.VoteCounts[alice.Id]); // Got 1 from Charlie
        Assert.Equal(5, round2.VoteCounts[bob.Id]); // Got 1 from Charlie
        Assert.Null(round2.EliminatedOption); // Final round, no more eliminations
    }

    #endregion

    #region Partial Ballot Tests

    [Fact]
    public void CalculateResult_PartialBallots_HandlesCorrectly()
    {
        // Arrange: Some voters only rank their top choice (partial ballots)
        var alice = new Option(Guid.NewGuid(), "Alice");
        var bob = new Option(Guid.NewGuid(), "Bob");
        var charlie = new Option(Guid.NewGuid(), "Charlie");

        var poll = new RankedChoicePoll(new[] { alice, bob, charlie });

        var ballots = new[]
        {
            // 5 for Alice (partial - only rank Alice)
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),

            // 3 for Bob (partial)
            new RankedBallot(new[] { bob.Id }),
            new RankedBallot(new[] { bob.Id }),
            new RankedBallot(new[] { bob.Id }),

            // 2 for Charlie with second choices
            new RankedBallot(new[] { charlie.Id, alice.Id }),
            new RankedBallot(new[] { charlie.Id, bob.Id })
        };

        // Act
        var calculator = new InstantRunoffCalculator();
        var result = poll.CalculateResult(ballots, calculator, new Random(42)); // Seeded for determinism

        // Assert
        Assert.Equal(alice, result.Winner);

        // When Charlie is eliminated, votes should transfer based on 2nd choice
        // One ballot has no 2nd choice and becomes "exhausted"
        Assert.Equal(2, result.Rounds.Count);
    }

    #endregion

    #region Single Round Winner Tests

    [Fact]
    public void CalculateResult_UnanimousVote_ReturnsWinnerInSingleRound()
    {
        // Arrange: Everyone votes for Alice
        var alice = new Option(Guid.NewGuid(), "Alice");
        var bob = new Option(Guid.NewGuid(), "Bob");

        var poll = new RankedChoicePoll(new[] { alice, bob });

        var ballots = new[]
        {
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id })
        };

        // Act
        var calculator = new InstantRunoffCalculator();
        var result = poll.CalculateResult(ballots, calculator, new Random(42)); // Seeded for determinism

        // Assert
        Assert.Equal(alice, result.Winner);
        Assert.False(result.IsTie);
        Assert.Single(result.Rounds);
        Assert.Equal(3, result.FinalVoteTotals[alice.Id]);
        Assert.Equal(0, result.FinalVoteTotals[bob.Id]);
    }

    #endregion

    #region Tie for Last Place Tests

    [Fact]
    public void CalculateResult_TieForLastPlace_RandomlyEliminatesOne()
    {
        // Arrange: Alice=4, Bob=3, Charlie=3 (Bob and Charlie tied for last, no majority)
        var alice = new Option(Guid.NewGuid(), "Alice");
        var bob = new Option(Guid.NewGuid(), "Bob");
        var charlie = new Option(Guid.NewGuid(), "Charlie");

        var poll = new RankedChoicePoll(new[] { alice, bob, charlie });

        var ballots = new[]
        {
            // 4 for Alice (40%, no majority)
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),
            new RankedBallot(new[] { alice.Id }),

            // 3 for Bob (tied for last)
            new RankedBallot(new[] { bob.Id, alice.Id }),
            new RankedBallot(new[] { bob.Id, alice.Id }),
            new RankedBallot(new[] { bob.Id, alice.Id }),

            // 3 for Charlie (tied for last)
            new RankedBallot(new[] { charlie.Id, alice.Id }),
            new RankedBallot(new[] { charlie.Id, alice.Id }),
            new RankedBallot(new[] { charlie.Id, alice.Id })
        };

        // Act - using seeded Random for deterministic test
        var calculator = new InstantRunoffCalculator();
        var result = poll.CalculateResult(ballots, calculator, new Random(42));

        // Assert
        Assert.Equal(alice, result.Winner); // Alice should win after one elimination
        Assert.False(result.IsTie);
        Assert.Equal(2, result.Rounds.Count);

        // Round 1: One of Bob or Charlie should be randomly eliminated
        var round1 = result.Rounds[0];
        Assert.Equal(4, round1.VoteCounts[alice.Id]);
        Assert.Equal(3, round1.VoteCounts[bob.Id]);
        Assert.Equal(3, round1.VoteCounts[charlie.Id]);
        Assert.NotNull(round1.EliminatedOption); // Either Bob or Charlie
        Assert.True(round1.EliminatedOption == bob || round1.EliminatedOption == charlie);

        // Round 2: Alice should have majority after getting votes from eliminated candidate
        var round2 = result.Rounds[1];
        Assert.Equal(7, round2.VoteCounts[alice.Id]); // 4 + 3 from eliminated candidate
        Assert.Null(round2.EliminatedOption);
    }

    #endregion

    #region Empty Ballots Tests

    [Fact]
    public void CalculateResult_NoBallots_ThrowsException()
    {
        // Arrange
        var alice = new Option(Guid.NewGuid(), "Alice");
        var bob = new Option(Guid.NewGuid(), "Bob");
        var poll = new RankedChoicePoll(new[] { alice, bob });
        var emptyBallots = Array.Empty<RankedBallot>();

        // Act & Assert
        var calculator = new InstantRunoffCalculator();
        var ex = Assert.Throws<InvalidOperationException>(() => poll.CalculateResult(emptyBallots, calculator));
        Assert.Contains("ballot", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
