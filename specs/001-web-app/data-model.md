# Data Model: Ranked Choice Voting Web Application

**Feature**: 001-web-app
**Date**: 2025-10-26
**Purpose**: Define entities, DTOs, domain models, and their mappings

## Model Architecture

This document defines four layers of models:

1. **Database Entities** (EF Core) - Maps to Azure SQL tables
2. **API DTOs** (Requests/Responses) - JSON contracts for REST API
3. **Core Domain Models** (Rcv.Core) - Existing voting logic models (DO NOT MODIFY)
4. **Frontend ViewModels** (Blazor) - UI-specific models

```
┌──────────────────────────────────────────────────────────────┐
│ Frontend ViewModels (Blazor)                                 │
│ - PollViewModel, VoteViewModel, ResultsViewModel            │
└──────────────────────────────────────────────────────────────┘
                        ↓ HTTP (JSON)
┌──────────────────────────────────────────────────────────────┐
│ API DTOs (Request/Response)                                  │
│ - CreatePollRequest, PollResponse, CastVoteRequest, etc.    │
└──────────────────────────────────────────────────────────────┘
                        ↓ Mapping Layer
┌──────────────┐                              ┌───────────────┐
│ Domain Models│                              │ EF Entities   │
│ (Rcv.Core)   │                              │ (Data Layer)  │
│ - Option     │◄─────────────────────────────│ - User        │
│ - RankedBallot│                             │ - Poll        │
│ - RcvResult  │                              │ - PollOption  │
│ - RoundSummary│                             │ - Vote        │
└──────────────┘                              └───────────────┘
                        ↓ EF Core
                ┌────────────────────┐
                │ Azure SQL Database │
                └────────────────────┘
```

---

## 1. Database Entities (EF Core)

These map 1:1 to database tables via Entity Framework Core.

### 1.1 User Entity

**File**: `src/Rcv.Web.Api/Data/Entities/User.cs`

```csharp
namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents an authenticated user from an OAuth provider.
/// Maps to Users table.
/// </summary>
public class User
{
    /// <summary>
    /// Primary key (GUID). Database-generated.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Provider's unique identifier for this user (e.g., Slack user ID).
    /// Combined with Provider forms unique constraint.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ExternalId { get; set; } = null!;

    /// <summary>
    /// OAuth provider name: 'Slack', 'Google', 'Microsoft', 'Apple', 'Teams'.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = null!;

    /// <summary>
    /// User's email address from OAuth provider (nullable if provider doesn't share).
    /// </summary>
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// User's display name from OAuth provider.
    /// </summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Timestamp when user first authenticated (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of user's most recent login (UTC).
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Poll> CreatedPolls { get; set; } = new List<Poll>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
```

**EF Core Configuration**:

```csharp
modelBuilder.Entity<User>(entity =>
{
    entity.ToTable("Users");
    entity.HasKey(u => u.Id);

    entity.HasIndex(u => new { u.ExternalId, u.Provider })
          .IsUnique()
          .HasDatabaseName("UQ_Users_ExternalId_Provider");

    entity.Property(u => u.CreatedAt)
          .HasDefaultValueSql("GETUTCDATE()");
});
```

---

### 1.2 Poll Entity

**File**: `src/Rcv.Web.Api/Data/Entities/Poll.cs`

```csharp
namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents a ranked choice voting poll.
/// Maps to Polls table.
/// </summary>
public class Poll
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key to poll creator (User.Id).
    /// </summary>
    [Required]
    public Guid CreatorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When poll voting closes (UTC). NULL = no deadline.
    /// </summary>
    public DateTime? ClosesAt { get; set; }

    /// <summary>
    /// When poll was actually closed (UTC). NULL = still active.
    /// Set when creator manually closes or ClosesAt is reached.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// True = results visible during voting (live results).
    /// False = results hidden until poll closes.
    /// </summary>
    public bool IsResultsPublic { get; set; } = true;

    /// <summary>
    /// True = individual votes and voter identities visible to creator.
    /// False = votes are anonymous (aggregate stats only).
    /// </summary>
    public bool IsVotingPublic { get; set; } = false;

    /// <summary>
    /// Poll status: 'Active', 'Closed', 'Deleted'.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    // Navigation properties
    public User Creator { get; set; } = null!;
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
```

**EF Core Configuration**:

```csharp
modelBuilder.Entity<Poll>(entity =>
{
    entity.ToTable("Polls");
    entity.HasKey(p => p.Id);

    entity.HasOne(p => p.Creator)
          .WithMany(u => u.CreatedPolls)
          .HasForeignKey(p => p.CreatorId)
          .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete polls when user deleted

    entity.HasIndex(p => p.CreatorId).HasDatabaseName("IX_Polls_CreatorId");
    entity.HasIndex(p => p.Status).HasDatabaseName("IX_Polls_Status");
    entity.HasIndex(p => p.CreatedAt).IsDescending().HasDatabaseName("IX_Polls_CreatedAt");

    entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
});
```

---

### 1.3 PollOption Entity

**File**: `src/Rcv.Web.Api/Data/Entities/PollOption.cs`

```csharp
namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents a single option/choice in a poll.
/// Maps to PollOptions table.
/// </summary>
public class PollOption
{
    public Guid Id { get; set; }

    [Required]
    public Guid PollId { get; set; }

    [Required]
    [MaxLength(200)]
    public string OptionText { get; set; } = null!;

    /// <summary>
    /// Order in which option is displayed to voters (1-indexed).
    /// </summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Poll Poll { get; set; } = null!;
}
```

**EF Core Configuration**:

```csharp
modelBuilder.Entity<PollOption>(entity =>
{
    entity.ToTable("PollOptions");
    entity.HasKey(po => po.Id);

    entity.HasOne(po => po.Poll)
          .WithMany(p => p.Options)
          .HasForeignKey(po => po.PollId)
          .OnDelete(DeleteBehavior.Cascade); // Delete options when poll deleted

    entity.HasIndex(po => po.PollId).HasDatabaseName("IX_PollOptions_PollId");

    entity.Property(po => po.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
});
```

---

### 1.4 Vote Entity

**File**: `src/Rcv.Web.Api/Data/Entities/Vote.cs`

```csharp
namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents a single user's ranked ballot in a poll.
/// Maps to Votes table.
/// </summary>
public class Vote
{
    public Guid Id { get; set; }

    [Required]
    public Guid PollId { get; set; }

    [Required]
    public Guid VoterId { get; set; }

    /// <summary>
    /// Ordered list of PollOption IDs from most to least preferred.
    /// Stored as JSON array: ["guid1", "guid2", "guid3"].
    /// EF Core 9 automatically serializes List<Guid> to JSON.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public List<Guid> RankedChoices { get; set; } = new();

    /// <summary>
    /// When vote was originally cast (UTC).
    /// </summary>
    public DateTime CastAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When vote was last updated (UTC). NULL if never updated.
    /// Note: Spec doesn't allow vote editing, but included for future extensibility.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Poll Poll { get; set; } = null!;
    public User Voter { get; set; } = null!;
}
```

**EF Core Configuration**:

```csharp
modelBuilder.Entity<Vote>(entity =>
{
    entity.ToTable("Votes");
    entity.HasKey(v => v.Id);

    entity.HasOne(v => v.Poll)
          .WithMany(p => p.Votes)
          .HasForeignKey(v => v.PollId)
          .OnDelete(DeleteBehavior.Cascade); // Delete votes when poll deleted

    entity.HasOne(v => v.Voter)
          .WithMany(u => u.Votes)
          .HasForeignKey(v => v.VoterId)
          .OnDelete(DeleteBehavior.Restrict); // Keep votes if user deleted (for audit)

    entity.HasIndex(v => new { v.PollId, v.VoterId })
          .IsUnique()
          .HasDatabaseName("UQ_Votes_PollId_VoterId"); // One vote per user per poll

    entity.HasIndex(v => v.PollId).HasDatabaseName("IX_Votes_PollId");
    entity.HasIndex(v => v.VoterId).HasDatabaseName("IX_Votes_VoterId");
    entity.HasIndex(v => v.CastAt).IsDescending().HasDatabaseName("IX_Votes_CastAt");

    // JSON column configuration
    entity.Property(v => v.RankedChoices)
          .HasConversion(
              v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
              v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
          );

    entity.Property(v => v.CastAt).HasDefaultValueSql("GETUTCDATE()");
});
```

---

## 2. Core Domain Models (Rcv.Core)

**DO NOT MODIFY** - These are from the existing Rcv.Core NuGet package.

### 2.1 Option (Rcv.Core)

```csharp
namespace Rcv.Core.Domain;

public record Option(Guid Id, string Label);
```

**Usage**: Represents a poll option in the voting algorithm.

**Mapping**: Created from `PollOption` entity:
```csharp
var option = new Option(pollOption.Id, pollOption.OptionText);
```

---

### 2.2 RankedBallot (Rcv.Core)

```csharp
namespace Rcv.Core.Domain;

public class RankedBallot
{
    public IReadOnlyList<Guid> RankedOptionIds { get; }

    public RankedBallot(IEnumerable<Guid> rankedOptionIds) { /* validation logic */ }
}
```

**Usage**: Represents a single voter's ranked preferences for RCV calculation.

**Mapping**: Created from `Vote` entity:
```csharp
var ballot = new RankedBallot(vote.RankedChoices);
```

---

### 2.3 RcvResult (Rcv.Core)

```csharp
namespace Rcv.Core.Domain;

public class RcvResult
{
    public Option? Winner { get; }
    public IReadOnlyList<RoundSummary> Rounds { get; }
    public bool IsTie { get; }
    public IReadOnlyList<Option> TiedOptions { get; }
    public IReadOnlyDictionary<Guid, int> FinalVoteTotals { get; }
}
```

**Usage**: Output from RCV calculation algorithm (via `RankedChoicePoll.CalculateResults()`).

**Mapping**: Mapped to `ResultsResponse` DTO for API.

---

### 2.4 RoundSummary (Rcv.Core)

```csharp
namespace Rcv.Core.Domain;

public class RoundSummary
{
    public int RoundNumber { get; }
    public IReadOnlyDictionary<Guid, int> VoteCounts { get; }
    public Option? EliminatedOption { get; }
}
```

**Usage**: Snapshot of vote distribution in a single elimination round.

**Mapping**: Included in `ResultsResponse.Rounds` array.

---

## 3. API DTOs (Request/Response)

These define the JSON contracts for the REST API.

### 3.1 Request DTOs

#### CreatePollRequest

**File**: `src/Rcv.Web.Api/Models/Requests/CreatePollRequest.cs`

```csharp
namespace Rcv.Web.Api.Models.Requests;

/// <summary>
/// Request to create a new poll.
/// </summary>
public record CreatePollRequest
{
    [Required(ErrorMessage = "Poll title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; init; } = null!;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "At least 2 options are required")]
    [MinLength(2, ErrorMessage = "A poll must have at least 2 options")]
    [MaxLength(20, ErrorMessage = "A poll cannot have more than 20 options")]
    public List<string> Options { get; init; } = new();

    /// <summary>
    /// When poll closes (UTC). NULL = no deadline.
    /// </summary>
    [FutureDate(ErrorMessage = "Poll end date must be in the future")]
    public DateTime? ClosesAt { get; init; }

    /// <summary>
    /// True = results visible during voting (live results).
    /// False = results hidden until poll closes.
    /// </summary>
    public bool IsResultsPublic { get; init; } = true;

    /// <summary>
    /// True = votes are public (creator can see who voted for what).
    /// False = votes are anonymous (aggregate stats only).
    /// </summary>
    public bool IsVotingPublic { get; init; } = false;
}
```

---

#### CastVoteRequest

**File**: `src/Rcv.Web.Api/Models/Requests/CastVoteRequest.cs`

```csharp
namespace Rcv.Web.Api.Models.Requests;

/// <summary>
/// Request to cast a ranked vote in a poll.
/// </summary>
public record CastVoteRequest
{
    /// <summary>
    /// Ordered list of option IDs from most to least preferred.
    /// Partial ballots are allowed (not all options must be ranked).
    /// </summary>
    [Required(ErrorMessage = "You must rank at least one option")]
    [MinLength(1, ErrorMessage = "You must rank at least one option")]
    public List<Guid> RankedOptionIds { get; init; } = new();
}
```

---

#### UpdatePollSettingsRequest

**File**: `src/Rcv.Web.Api/Models/Requests/UpdatePollSettingsRequest.cs`

```csharp
namespace Rcv.Web.Api.Models.Requests;

/// <summary>
/// Request to update poll settings (only creator can update).
/// </summary>
public record UpdatePollSettingsRequest
{
    /// <summary>
    /// True = results visible during voting.
    /// </summary>
    public bool? IsResultsPublic { get; init; }

    /// <summary>
    /// True = votes are public.
    /// </summary>
    public bool? IsVotingPublic { get; init; }
}
```

---

### 3.2 Response DTOs

#### PollResponse

**File**: `src/Rcv.Web.Api/Models/Responses/PollResponse.cs`

```csharp
namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Poll details returned by API.
/// </summary>
public record PollResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public UserResponse Creator { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime? ClosesAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string Status { get; init; } = "Active";
    public bool IsResultsPublic { get; init; }
    public bool IsVotingPublic { get; init; }
    public List<PollOptionResponse> Options { get; init; } = new();
    public int TotalVotes { get; init; }

    /// <summary>
    /// True if current user has voted in this poll.
    /// Only populated when request includes authenticated user context.
    /// </summary>
    public bool? CurrentUserHasVoted { get; init; }
}

public record PollOptionResponse
{
    public Guid Id { get; init; }
    public string Text { get; init; } = null!;
    public int DisplayOrder { get; init; }
}
```

---

#### VoteResponse

**File**: `src/Rcv.Web.Api/Models/Responses/VoteResponse.cs`

```csharp
namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Vote confirmation returned after casting vote.
/// </summary>
public record VoteResponse
{
    public Guid Id { get; init; }
    public Guid PollId { get; init; }
    public DateTime CastAt { get; init; }

    /// <summary>
    /// Only populated if poll is public and requester is poll creator.
    /// </summary>
    public List<Guid>? RankedChoices { get; init; }
}
```

---

#### ResultsResponse

**File**: `src/Rcv.Web.Api/Models/Responses/ResultsResponse.cs`

```csharp
namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// RCV poll results with round-by-round elimination data.
/// </summary>
public record ResultsResponse
{
    public Guid PollId { get; init; }
    public string PollTitle { get; init; } = null!;
    public int TotalVotes { get; init; }

    /// <summary>
    /// Winning option (null if tie).
    /// </summary>
    public PollOptionResponse? Winner { get; init; }

    /// <summary>
    /// True if election ended in a tie.
    /// </summary>
    public bool IsTie { get; init; }

    /// <summary>
    /// Options that tied for the win (empty if IsTie is false).
    /// </summary>
    public List<PollOptionResponse> TiedOptions { get; init; } = new();

    /// <summary>
    /// Round-by-round elimination data.
    /// </summary>
    public List<RoundSummaryResponse> Rounds { get; init; } = new();

    /// <summary>
    /// Final vote totals by option.
    /// </summary>
    public Dictionary<Guid, int> FinalVoteTotals { get; init; } = new();

    /// <summary>
    /// Participation statistics.
    /// </summary>
    public ParticipationStats Participation { get; init; } = null!;
}

public record RoundSummaryResponse
{
    public int RoundNumber { get; init; }
    public Dictionary<Guid, int> VoteCounts { get; init; } = new();
    public PollOptionResponse? EliminatedOption { get; init; }
}

public record ParticipationStats
{
    public int TotalVotesCast { get; init; }
    public double ParticipationRate { get; init; } // Percentage (0-100)
}
```

---

#### UserResponse

**File**: `src/Rcv.Web.Api/Models/Responses/UserResponse.cs`

```csharp
namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// User profile information.
/// </summary>
public record UserResponse
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = null!;
    public string? Email { get; init; }
    public string Provider { get; init; } = null!;
}
```

---

## 4. Frontend ViewModels (Blazor)

These are UI-specific models used in Blazor components.

### 4.1 PollViewModel

**File**: `src/Rcv.Web.Client/Models/PollViewModel.cs`

```csharp
namespace Rcv.Web.Client.Models;

/// <summary>
/// View model for displaying poll details in UI.
/// </summary>
public class PollViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string CreatorName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsResultsPublic { get; set; }
    public bool IsVotingPublic { get; set; }
    public List<OptionViewModel> Options { get; set; } = new();
    public int TotalVotes { get; set; }
    public bool CurrentUserHasVoted { get; set; }

    // UI-specific computed properties
    public bool IsActive => Status == "Active" && (ClosesAt == null || ClosesAt > DateTime.UtcNow);
    public bool IsClosed => Status == "Closed" || (ClosesAt.HasValue && ClosesAt <= DateTime.UtcNow);
    public string TimeRemaining => GetTimeRemaining();

    private string GetTimeRemaining()
    {
        if (!ClosesAt.HasValue || IsClosed) return "Closed";
        var remaining = ClosesAt.Value - DateTime.UtcNow;
        if (remaining.TotalDays > 1) return $"{(int)remaining.TotalDays} days left";
        if (remaining.TotalHours > 1) return $"{(int)remaining.TotalHours} hours left";
        return $"{(int)remaining.TotalMinutes} minutes left";
    }
}

public class OptionViewModel
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
    public int Rank { get; set; } = 0; // 0 = not ranked, 1 = first choice, etc.
}
```

---

### 4.2 VoteViewModel

**File**: `src/Rcv.Web.Client/Models/VoteViewModel.cs`

```csharp
namespace Rcv.Web.Client.Models;

/// <summary>
/// View model for voting interface.
/// </summary>
public class VoteViewModel
{
    public Guid PollId { get; set; }
    public List<OptionViewModel> Options { get; set; } = new();
    public List<Guid> RankedChoices { get; set; } = new();

    // UI helpers
    public int GetRank(Guid optionId) =>
        RankedChoices.IndexOf(optionId) + 1; // 0 if not ranked, 1+ if ranked

    public void SetRank(Guid optionId, int rank)
    {
        RankedChoices.Remove(optionId);
        if (rank > 0)
            RankedChoices.Insert(rank - 1, optionId);
    }

    public bool IsValid => RankedChoices.Count > 0;
}
```

---

### 4.3 ResultsViewModel

**File**: `src/Rcv.Web.Client/Models/ResultsViewModel.cs`

```csharp
namespace Rcv.Web.Client.Models;

/// <summary>
/// View model for displaying RCV results.
/// </summary>
public class ResultsViewModel
{
    public string PollTitle { get; set; } = null!;
    public int TotalVotes { get; set; }
    public OptionViewModel? Winner { get; set; }
    public bool IsTie { get; set; }
    public List<OptionViewModel> TiedOptions { get; set; } = new();
    public List<RoundViewModel> Rounds { get; set; } = new();
    public Dictionary<Guid, int> FinalVoteTotals { get; set; } = new();
    public double ParticipationRate { get; set; }

    // UI-specific properties
    public string WinnerMessage => IsTie
        ? $"Tie between: {string.Join(", ", TiedOptions.Select(o => o.Text))}"
        : $"Winner: {Winner?.Text}";
}

public class RoundViewModel
{
    public int RoundNumber { get; set; }
    public Dictionary<Guid, int> VoteCounts { get; set; } = new();
    public OptionViewModel? EliminatedOption { get; set; }
    public string Summary => EliminatedOption != null
        ? $"Round {RoundNumber}: {EliminatedOption.Text} eliminated"
        : $"Round {RoundNumber}: Final results";
}
```

---

## 5. Mapping Layer

Mappers convert between entity ↔ DTO ↔ domain model ↔ view model.

### 5.1 PollMapper

**File**: `src/Rcv.Web.Api/Mapping/PollMapper.cs`

```csharp
namespace Rcv.Web.Api.Mapping;

public static class PollMapper
{
    /// <summary>
    /// Maps Poll entity to PollResponse DTO.
    /// </summary>
    public static PollResponse ToResponse(Poll poll, Guid? currentUserId = null)
    {
        return new PollResponse
        {
            Id = poll.Id,
            Title = poll.Title,
            Description = poll.Description,
            Creator = UserMapper.ToResponse(poll.Creator),
            CreatedAt = poll.CreatedAt,
            ClosesAt = poll.ClosesAt,
            ClosedAt = poll.ClosedAt,
            Status = poll.Status,
            IsResultsPublic = poll.IsResultsPublic,
            IsVotingPublic = poll.IsVotingPublic,
            Options = poll.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(ToPollOptionResponse)
                .ToList(),
            TotalVotes = poll.Votes.Count,
            CurrentUserHasVoted = currentUserId.HasValue
                ? poll.Votes.Any(v => v.VoterId == currentUserId.Value)
                : null
        };
    }

    /// <summary>
    /// Maps PollOption entity to PollOptionResponse DTO.
    /// </summary>
    public static PollOptionResponse ToPollOptionResponse(PollOption option)
    {
        return new PollOptionResponse
        {
            Id = option.Id,
            Text = option.OptionText,
            DisplayOrder = option.DisplayOrder
        };
    }

    /// <summary>
    /// Maps PollOption entity to Rcv.Core Option domain model.
    /// </summary>
    public static Option ToDomainOption(PollOption option)
    {
        return new Option(option.Id, option.OptionText);
    }
}
```

---

### 5.2 VoteMapper

**File**: `src/Rcv.Web.Api/Mapping/VoteMapper.cs`

```csharp
namespace Rcv.Web.Api.Mapping;

public static class VoteMapper
{
    /// <summary>
    /// Maps Vote entity to VoteResponse DTO.
    /// </summary>
    public static VoteResponse ToResponse(Vote vote, bool includeRankedChoices = false)
    {
        return new VoteResponse
        {
            Id = vote.Id,
            PollId = vote.PollId,
            CastAt = vote.CastAt,
            RankedChoices = includeRankedChoices ? vote.RankedChoices : null
        };
    }

    /// <summary>
    /// Maps Vote entity to Rcv.Core RankedBallot domain model.
    /// </summary>
    public static RankedBallot ToDomainBallot(Vote vote)
    {
        return new RankedBallot(vote.RankedChoices);
    }
}
```

---

### 5.3 ResultsMapper

**File**: `src/Rcv.Web.Api/Mapping/ResultsMapper.cs`

```csharp
namespace Rcv.Web.Api.Mapping;

public static class ResultsMapper
{
    /// <summary>
    /// Maps Rcv.Core RcvResult domain model to ResultsResponse DTO.
    /// </summary>
    public static ResultsResponse ToResponse(
        RcvResult result,
        Poll poll,
        List<PollOption> options)
    {
        var optionLookup = options.ToDictionary(o => o.Id, PollMapper.ToPollOptionResponse);

        return new ResultsResponse
        {
            PollId = poll.Id,
            PollTitle = poll.Title,
            TotalVotes = poll.Votes.Count,
            Winner = result.Winner != null ? optionLookup[result.Winner.Id] : null,
            IsTie = result.IsTie,
            TiedOptions = result.TiedOptions
                .Select(o => optionLookup[o.Id])
                .ToList(),
            Rounds = result.Rounds
                .Select(r => ToRoundSummaryResponse(r, optionLookup))
                .ToList(),
            FinalVoteTotals = result.FinalVoteTotals.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Participation = new ParticipationStats
            {
                TotalVotesCast = poll.Votes.Count,
                ParticipationRate = 100.0 // Could calculate from invite list if implemented
            }
        };
    }

    private static RoundSummaryResponse ToRoundSummaryResponse(
        RoundSummary round,
        Dictionary<Guid, PollOptionResponse> optionLookup)
    {
        return new RoundSummaryResponse
        {
            RoundNumber = round.RoundNumber,
            VoteCounts = round.VoteCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            EliminatedOption = round.EliminatedOption != null
                ? optionLookup[round.EliminatedOption.Id]
                : null
        };
    }
}
```

---

## 6. Data Flow Examples

### Example 1: Creating a Poll

```
CreatePollRequest (API DTO)
    ↓ Controller validates & extracts data
Poll + PollOption entities (EF Core)
    ↓ EF Core saves to database
    ↓ Service returns entity
PollResponse (API DTO)
    ↓ HTTP response (JSON)
PollViewModel (Blazor)
    ↓ UI displays poll
```

---

### Example 2: Casting a Vote

```
CastVoteRequest (API DTO with RankedOptionIds)
    ↓ Controller validates
Vote entity (EF Core with RankedChoices JSON)
    ↓ EF Core saves to database
VoteResponse (API DTO)
    ↓ HTTP response (JSON)
VoteViewModel (Blazor)
    ↓ UI shows confirmation
```

---

### Example 3: Calculating Results

```
Poll + Votes entities (EF Core)
    ↓ Service fetches from database
    ↓ Map to domain models
List<Option> + List<RankedBallot> (Rcv.Core domain)
    ↓ Call RankedChoicePoll.CalculateResults()
RcvResult (Rcv.Core domain)
    ↓ Map to DTO
ResultsResponse (API DTO)
    ↓ HTTP response (JSON)
ResultsViewModel (Blazor)
    ↓ UI displays winner & rounds
```

---

## Summary

**Model Count**:
- Database Entities: 4 (User, Poll, PollOption, Vote)
- Core Domain Models: 4 (Option, RankedBallot, RcvResult, RoundSummary) - EXISTING, DO NOT MODIFY
- API Request DTOs: 3 (CreatePollRequest, CastVoteRequest, UpdatePollSettingsRequest)
- API Response DTOs: 6 (PollResponse, VoteResponse, ResultsResponse, UserResponse, + nested)
- Frontend ViewModels: 3 (PollViewModel, VoteViewModel, ResultsViewModel)

**Key Mappings**:
- `Poll` entity → `PollResponse` DTO → `PollViewModel`
- `Vote` entity → `RankedBallot` domain model (for calculation)
- `RcvResult` domain model → `ResultsResponse` DTO → `ResultsViewModel`
- `PollOption` entity → `Option` domain model (for calculation)

All models are immutable where possible (using records and readonly collections). Validation occurs at multiple layers: API binding, service logic, and domain model constructors.
