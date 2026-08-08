using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Implements poll lifecycle management: creation, retrieval, updates, closure, and deletion.
/// </summary>
public class PollService : IPollService
{
    private readonly RcvDbContext _context;
    private readonly IPollLifecycleService _lifecycle;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes the service with system lifecycle dependencies.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PollService(RcvDbContext context)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        _context = context;
        _cache = cache;
        _lifecycle = new PollLifecycleService(context, TimeProvider.System, cache);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PollService"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="lifecycle">The effective-status lifecycle service.</param>
    /// <param name="cache">The results cache.</param>
    public PollService(
        RcvDbContext context,
        IPollLifecycleService lifecycle,
        IMemoryCache cache)
    {
        _context = context;
        _lifecycle = lifecycle;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<PollResponse> CreatePollAsync(Guid creatorId, CreatePollRequest request)
    {
        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            Title = request.Title,
            Description = request.Description,
            ClosesAt = request.ClosesAt,
            IsResultsPublic = request.IsResultsPublic,
            Status = PollStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Polls.Add(poll);

        var options = request.Options
            .Select((text, index) => new PollOption
            {
                Id = Guid.NewGuid(),
                PollId = poll.Id,
                OptionText = text,
                DisplayOrder = index,
                CreatedAt = DateTime.UtcNow,
            })
            .ToList();

        _context.PollOptions.AddRange(options);
        await _context.SaveChangesAsync();

        return (await GetPollByIdAsync(poll.Id))!;
    }

    /// <inheritdoc />
    public async Task<PollResponse?> GetPollByIdAsync(Guid pollId)
    {
        var poll = await _context.Polls
            .Include(p => p.Creator)
            .Include(p => p.Options)
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll is null || poll.Status == PollStatus.Deleted)
            return null;

        await _lifecycle.ApplyEffectiveStatusAsync(poll);
        return MapToPollResponse(poll);
    }

    /// <inheritdoc />
    public async Task<PollListResponse> GetPollsByCreatorAsync(
        Guid creatorId,
        int page,
        int pageSize,
        PollStatus? status = null)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        if (status.HasValue &&
            (!Enum.IsDefined(status.Value) || status == PollStatus.Deleted))
            throw new ArgumentOutOfRangeException(nameof(status), "Status must be Active or Closed.");

        await _lifecycle.ApplyEffectiveStatusesForCreatorAsync(creatorId);

        var query = _context.Polls
            .AsNoTracking()
            .Where(p => p.CreatorId == creatorId && p.Status != PollStatus.Deleted);
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var totalCount = await query.CountAsync();
        var pageItems = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Creator)
            .Include(p => p.Options)
            .Include(p => p.Votes)
            .AsSplitQuery()
            .ToListAsync();

        return new PollListResponse
        {
            Items = pageItems.Select(MapToPollResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    /// <inheritdoc />
    public async Task<PollResponse> ClosePollAsync(Guid pollId, Guid requestingUserId)
    {
        var poll = await _context.Polls.FindAsync(pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        await _lifecycle.ApplyEffectiveStatusAsync(poll);
        if (poll.CreatorId != requestingUserId)
            throw new UnauthorizedAccessException("Only the poll creator can close the poll.");

        if (poll.Status != PollStatus.Active)
            throw new InvalidOperationException($"Poll cannot be closed because its current status is '{poll.Status}'.");

        poll.Status = PollStatus.Closed;
        poll.ClosedAt = DateTime.UtcNow;
        _cache.Remove(ResultsCacheKeys.ForPoll(poll.Id));
        await _context.SaveChangesAsync();

        return (await GetPollByIdAsync(poll.Id))!;
    }

    /// <inheritdoc />
    public async Task DeletePollAsync(Guid pollId, Guid requestingUserId)
    {
        var poll = await _context.Polls.FindAsync(pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        await _lifecycle.ApplyEffectiveStatusAsync(poll);
        if (poll.CreatorId != requestingUserId)
            throw new UnauthorizedAccessException("Only the poll creator can delete the poll.");

        poll.Status = PollStatus.Deleted;
        _cache.Remove(ResultsCacheKeys.ForPoll(poll.Id));
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<PollResponse> UpdatePollAsync(Guid pollId, Guid requestingUserId, UpdatePollRequest request)
    {
        var poll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        await _lifecycle.ApplyEffectiveStatusAsync(poll);
        if (poll.CreatorId != requestingUserId)
            throw new UnauthorizedAccessException("Only the poll creator can update the poll.");

        if (poll.Status != PollStatus.Active)
            throw new InvalidOperationException($"Poll cannot be updated because its current status is '{poll.Status}'.");

        var voteCount = await _context.Votes.CountAsync(v => v.PollId == poll.Id);
        if (voteCount > 0)
            throw new InvalidOperationException("Cannot update poll after votes have been cast.");

        if (request.Title != null)
            poll.Title = request.Title;

        if (request.Description != null)
            poll.Description = request.Description;

        if (request.Options != null)
        {
            _context.PollOptions.RemoveRange(poll.Options);

            var newOptions = request.Options
                .Select((text, index) => new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = poll.Id,
                    OptionText = text,
                    DisplayOrder = index,
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList();

            _context.PollOptions.AddRange(newOptions);
        }

        if (request.RemoveClosesAt)
            poll.ClosesAt = null;
        else if (request.ClosesAt != null)
            poll.ClosesAt = request.ClosesAt;

        if (request.IsResultsPublic.HasValue)
            poll.IsResultsPublic = request.IsResultsPublic.Value;

        _cache.Remove(ResultsCacheKeys.ForPoll(poll.Id));
        await _context.SaveChangesAsync();

        return (await GetPollByIdAsync(poll.Id))!;
    }

    /// <summary>
    /// Maps a <see cref="Poll"/> entity (with Creator, Options, and Votes loaded) to a <see cref="PollResponse"/>.
    /// </summary>
    /// <param name="poll">The poll entity to map. Must have Creator, Options, and Votes navigation properties loaded.</param>
    /// <returns>A fully populated <see cref="PollResponse"/>.</returns>
    private static PollResponse MapToPollResponse(Poll poll) => new()
    {
        Id = poll.Id,
        Title = poll.Title,
        Description = poll.Description,
        Creator = new UserSummaryDto(poll.Creator.Id, poll.Creator.DisplayName),
        Options = poll.Options
            .OrderBy(o => o.DisplayOrder)
            .Select(o => new PollOptionDto(o.Id, o.OptionText, o.DisplayOrder))
            .ToList(),
        Status = poll.Status,
        CreatedAt = poll.CreatedAt,
        ClosesAt = poll.ClosesAt,
        ClosedAt = poll.ClosedAt,
        IsResultsPublic = poll.IsResultsPublic,
        VoteCount = poll.Votes.Count,
    };
}
