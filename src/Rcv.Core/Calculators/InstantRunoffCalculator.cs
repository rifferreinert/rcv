using Rcv.Core.Domain;

namespace Rcv.Core.Calculators;

/// <summary>
/// Instant-runoff voting (IRV) calculator.
/// Eliminates lowest-vote candidates round-by-round until someone achieves majority.
/// When multiple candidates tie for last place, one is randomly selected for elimination.
/// </summary>
public class InstantRunoffCalculator : IRcvCalculator
{
    /// <inheritdoc />
    public RcvResult Calculate(IReadOnlyList<Option> options, IEnumerable<RankedBallot> ballots, Random? random = null)
    {
        var rng = random ?? new Random();
        var ballotList = ballots.ToList();

        if (ballotList.Count == 0)
        {
            throw new InvalidOperationException("Cannot calculate results with no ballots");
        }

        var rounds = new List<RoundSummary>();
        var remainingCandidates = options.Select(o => o.Id).ToHashSet();
        int roundNumber = 1;

        while (true)
        {
            // Count votes for each remaining candidate
            var voteCounts = CountVotes(ballotList, remainingCandidates);

            // Check if we have a winner (>50% of remaining votes)
            var totalVotes = voteCounts.Values.Sum();
            var majority = totalVotes / 2.0;

            var candidateWithMajority = voteCounts.FirstOrDefault(kvp => kvp.Value > majority);

            if (candidateWithMajority.Key != Guid.Empty)
            {
                // We have a winner!
                var winner = options.First(o => o.Id == candidateWithMajority.Key);
                rounds.Add(new RoundSummary(roundNumber, voteCounts.AsReadOnly(), null)); // No elimination in final round

                return new RcvResult(
                    winner,
                    rounds,
                    voteCounts.AsReadOnly(),
                    null); // No tie
            }

            // Check if we have a tie (all remaining candidates have equal votes)
            if (voteCounts.Values.Distinct().Count() == 1 && voteCounts.Count > 1)
            {
                // Perfect tie among all remaining candidates
                var tiedOptions = voteCounts.Keys.Select(id => options.First(o => o.Id == id)).ToList();
                rounds.Add(new RoundSummary(roundNumber, voteCounts.AsReadOnly(), null)); // No elimination

                return new RcvResult(
                    null, // No winner
                    rounds,
                    voteCounts.AsReadOnly(),
                    tiedOptions);
            }

            // No winner yet - eliminate the candidate with fewest votes
            var lowestVoteCount = voteCounts.Values.Min();
            var candidatesWithLowestVotes = voteCounts
                .Where(kvp => kvp.Value == lowestVoteCount)
                .Select(kvp => kvp.Key)
                .ToList();

            // Handle tie for last place by random selection
            Guid eliminatedCandidateId;
            if (candidatesWithLowestVotes.Count > 1)
            {
                // Random tie-breaking
                var randomIndex = rng.Next(candidatesWithLowestVotes.Count);
                eliminatedCandidateId = candidatesWithLowestVotes[randomIndex];
            }
            else
            {
                eliminatedCandidateId = candidatesWithLowestVotes[0];
            }

            var eliminatedOption = options.First(o => o.Id == eliminatedCandidateId);
            rounds.Add(new RoundSummary(roundNumber, voteCounts.AsReadOnly(), eliminatedOption));

            // Remove eliminated candidate
            remainingCandidates.Remove(eliminatedCandidateId);

            // Check if only one candidate remains
            if (remainingCandidates.Count == 1)
            {
                // Last candidate standing wins
                var winnerId = remainingCandidates.Single();
                var winner = options.First(o => o.Id == winnerId);

                // Do one more round to show final vote counts
                roundNumber++;
                var finalVoteCounts = CountVotes(ballotList, remainingCandidates);
                rounds.Add(new RoundSummary(roundNumber, finalVoteCounts.AsReadOnly(), null));

                return new RcvResult(winner, rounds, finalVoteCounts.AsReadOnly(), null);
            }

            roundNumber++;
        }
    }

    /// <summary>
    /// Count first-choice votes for each remaining candidate.
    /// Ballots are counted for the highest-ranked candidate who hasn't been eliminated.
    /// </summary>
    private Dictionary<Guid, int> CountVotes(List<RankedBallot> ballots, HashSet<Guid> remainingCandidates)
    {
        var counts = remainingCandidates.ToDictionary(id => id, id => 0);

        foreach (var ballot in ballots)
        {
            // Find the first choice on this ballot who is still in the race
            var topChoice = ballot.RankedOptionIds.FirstOrDefault(id => remainingCandidates.Contains(id));

            if (topChoice != Guid.Empty)
            {
                counts[topChoice]++;
            }
            // If topChoice is Empty, this is an exhausted ballot (no remaining preferences)
        }

        return counts;
    }
}
