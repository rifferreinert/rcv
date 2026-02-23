using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Services;

namespace Rcv.Web.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PollService"/>. Uses an in-memory database
/// so no real SQL Server connection is required.
/// </summary>
public class PollServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an isolated in-memory <see cref="RcvDbContext"/> for each test.
    /// Each test gets its own database name to prevent cross-test data leakage.
    /// </summary>
    private static RcvDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RcvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RcvDbContext(options);
    }

    /// <summary>
    /// Creates and saves a test <see cref="User"/> to the given context.
    /// </summary>
    private static async Task<User> CreateTestUser(RcvDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = Guid.NewGuid().ToString(),
            Provider = "Google",
            Email = "testuser@example.com",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Builds a sample <see cref="CreatePollRequest"/> with a title, description,
    /// and three options, suitable for most test scenarios.
    /// </summary>
    private static CreatePollRequest BuildCreatePollRequest() =>
        new CreatePollRequest
        {
            Title = "Best Programming Language?",
            Description = "Vote for your favourite language.",
            Options = new List<string> { "C#", "Python", "TypeScript" },
        };

    private static IPollService CreateService(RcvDbContext context) =>
        new PollService(context);

    // -----------------------------------------------------------------------
    // CreatePollAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreatePollAsync_ValidRequest_ReturnsPollResponse()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var request = BuildCreatePollRequest();

        var result = await service.CreatePollAsync(user.Id, request);

        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);
        result.Description.Should().Be(request.Description);
        result.Status.Should().Be("Active");
        result.Creator.Id.Should().Be(user.Id);
        result.Creator.Email.Should().Be(user.Email);
        result.Creator.DisplayName.Should().Be(user.DisplayName);
        result.Options.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreatePollAsync_SetsDisplayOrder_ByListPosition()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var request = BuildCreatePollRequest(); // options: "C#", "Python", "TypeScript"

        var result = await service.CreatePollAsync(user.Id, request);

        var ordered = result.Options.OrderBy(o => o.DisplayOrder).ToList();
        ordered[0].DisplayOrder.Should().Be(0);
        ordered[0].Text.Should().Be("C#");
        ordered[1].DisplayOrder.Should().Be(1);
        ordered[1].Text.Should().Be("Python");
        ordered[2].DisplayOrder.Should().Be(2);
        ordered[2].Text.Should().Be("TypeScript");
    }

    [Fact]
    public async Task CreatePollAsync_SetsPollId_ToNewGuid()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var result = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreatePollAsync_SetsCreatedAt_ToUtcNow()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var before = DateTime.UtcNow;
        var result = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());
        var after = DateTime.UtcNow;

        result.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    // -----------------------------------------------------------------------
    // GetPollByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPollByIdAsync_ExistingPoll_ReturnsPollWithOptions()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        var result = await service.GetPollByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Options.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPollByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var result = await service.GetPollByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPollByIdAsync_DeletedPoll_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        // Soft-delete the poll directly in the DB
        var poll = await context.Polls.FindAsync(created.Id);
        poll!.Status = "Deleted";
        await context.SaveChangesAsync();

        var result = await service.GetPollByIdAsync(created.Id);

        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetPollsByCreatorAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPollsByCreatorAsync_ReturnsOnlyCreatorPolls()
    {
        using var context = CreateInMemoryContext();
        var userA = await CreateTestUser(context);
        var userB = await CreateTestUser(context);
        var service = CreateService(context);

        await service.CreatePollAsync(userA.Id, BuildCreatePollRequest());
        await service.CreatePollAsync(userA.Id, BuildCreatePollRequest());
        await service.CreatePollAsync(userB.Id, BuildCreatePollRequest());

        var result = await service.GetPollsByCreatorAsync(userA.Id, page: 1, pageSize: 10);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Creator.Id.Should().Be(userA.Id));
    }

    [Fact]
    public async Task GetPollsByCreatorAsync_ExcludesDeletedPolls()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var poll1 = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());
        var poll2 = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        // Soft-delete the second poll
        var entity = await context.Polls.FindAsync(poll2.Id);
        entity!.Status = "Deleted";
        await context.SaveChangesAsync();

        var result = await service.GetPollsByCreatorAsync(user.Id, page: 1, pageSize: 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(poll1.Id);
    }

    // -----------------------------------------------------------------------
    // GetActivePollsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetActivePollsAsync_ReturnsOnlyActivePolls()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var active = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        // Create a closed poll directly
        var closedPoll = new Poll
        {
            Id = Guid.NewGuid(),
            Title = "Closed Poll",
            CreatorId = user.Id,
            Status = "Closed",
            CreatedAt = DateTime.UtcNow,
        };
        context.Polls.Add(closedPoll);

        // Create a deleted poll directly
        var deletedPoll = new Poll
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Poll",
            CreatorId = user.Id,
            Status = "Deleted",
            CreatedAt = DateTime.UtcNow,
        };
        context.Polls.Add(deletedPoll);
        await context.SaveChangesAsync();

        var result = await service.GetActivePollsAsync(page: 1, pageSize: 10);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task GetActivePollsAsync_PaginatesCorrectly()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        for (int i = 0; i < 5; i++)
            await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        var result = await service.GetActivePollsAsync(page: 1, pageSize: 2);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    // -----------------------------------------------------------------------
    // ClosePollAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ClosePollAsync_ByCreator_SetsStatusAndClosedAt()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        var before = DateTime.UtcNow;
        var result = await service.ClosePollAsync(created.Id, user.Id);
        var after = DateTime.UtcNow;

        result.Status.Should().Be("Closed");
        result.ClosedAt.Should().NotBeNull();
        result.ClosedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task ClosePollAsync_ByNonCreator_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateInMemoryContext();
        var creator = await CreateTestUser(context);
        var other = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(creator.Id, BuildCreatePollRequest());

        var act = async () => await service.ClosePollAsync(created.Id, other.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ClosePollAsync_AlreadyClosed_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        await service.ClosePollAsync(created.Id, user.Id);

        var act = async () => await service.ClosePollAsync(created.Id, user.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // -----------------------------------------------------------------------
    // DeletePollAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeletePollAsync_ByCreator_SetsStatusDeleted()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        await service.DeletePollAsync(created.Id, user.Id);

        var poll = await context.Polls.FindAsync(created.Id);
        poll!.Status.Should().Be("Deleted");
    }

    [Fact]
    public async Task DeletePollAsync_ByNonCreator_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateInMemoryContext();
        var creator = await CreateTestUser(context);
        var other = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(creator.Id, BuildCreatePollRequest());

        var act = async () => await service.DeletePollAsync(created.Id, other.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // -----------------------------------------------------------------------
    // UpdatePollAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePollAsync_BeforeVotes_UpdatesTitle()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        var updateRequest = new UpdatePollRequest { Title = "Updated Title" };
        var result = await service.UpdatePollAsync(created.Id, user.Id, updateRequest);

        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdatePollAsync_BeforeVotes_UpdatesOptions()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        var updateRequest = new UpdatePollRequest
        {
            Options = new List<string> { "Rust", "Go" },
        };
        var result = await service.UpdatePollAsync(created.Id, user.Id, updateRequest);

        result.Options.Should().HaveCount(2);
        result.Options.Select(o => o.Text).Should().BeEquivalentTo(new[] { "Rust", "Go" });
    }

    [Fact]
    public async Task UpdatePollAsync_AfterVotesAreCast_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        // Add a vote directly to the DB to simulate that votes have been cast
        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            PollId = created.Id,
            VoterId = user.Id,
            RankedChoices = new List<Guid> { created.Options[0].Id },
            CastAt = DateTime.UtcNow,
        };
        context.Votes.Add(vote);
        await context.SaveChangesAsync();

        var act = async () => await service.UpdatePollAsync(
            created.Id, user.Id, new UpdatePollRequest { Title = "New Title" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdatePollAsync_ByNonCreator_ThrowsUnauthorizedAccessException()
    {
        using var context = CreateInMemoryContext();
        var creator = await CreateTestUser(context);
        var other = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(creator.Id, BuildCreatePollRequest());

        var act = async () => await service.UpdatePollAsync(
            created.Id, other.Id, new UpdatePollRequest { Title = "New Title" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // -----------------------------------------------------------------------
    // CreatePollAsync — settings are persisted
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreatePollAsync_PersistsSettings_ClosesAtIsResultsPublicIsVotingPublic()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var closesAt = DateTime.UtcNow.AddDays(7);
        var request = new CreatePollRequest
        {
            Title = "Settings Test Poll",
            Options = new List<string> { "A", "B" },
            ClosesAt = closesAt,
            IsResultsPublic = false,
            IsVotingPublic = true,
        };

        var result = await service.CreatePollAsync(user.Id, request);

        result.ClosesAt.Should().BeCloseTo(closesAt, precision: TimeSpan.FromSeconds(1));
        result.IsResultsPublic.Should().BeFalse();
        result.IsVotingPublic.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // GetPollByIdAsync — closed poll IS visible
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPollByIdAsync_ClosedPoll_ReturnsPoll()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        var poll = await context.Polls.FindAsync(created.Id);
        poll!.Status = "Closed";
        await context.SaveChangesAsync();

        var result = await service.GetPollByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Closed");
    }

    // -----------------------------------------------------------------------
    // GetPollsByCreatorAsync — includes closed polls
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPollsByCreatorAsync_IncludesClosedPolls()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var poll1 = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());
        var poll2 = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        var entity = await context.Polls.FindAsync(poll2.Id);
        entity!.Status = "Closed";
        await context.SaveChangesAsync();

        var result = await service.GetPollsByCreatorAsync(user.Id, page: 1, pageSize: 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    // -----------------------------------------------------------------------
    // ClosePollAsync — unknown ID throws KeyNotFoundException
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ClosePollAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var act = async () => await service.ClosePollAsync(Guid.NewGuid(), user.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // -----------------------------------------------------------------------
    // DeletePollAsync — unknown ID throws KeyNotFoundException
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeletePollAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var act = async () => await service.DeletePollAsync(Guid.NewGuid(), user.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // -----------------------------------------------------------------------
    // UpdatePollAsync — unknown ID throws KeyNotFoundException
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePollAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var act = async () => await service.UpdatePollAsync(
            Guid.NewGuid(), user.Id, new UpdatePollRequest { Title = "New Title" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // -----------------------------------------------------------------------
    // UpdatePollAsync — closed poll throws InvalidOperationException
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePollAsync_OnClosedPoll_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var created = await service.CreatePollAsync(user.Id, BuildCreatePollRequest());

        await service.ClosePollAsync(created.Id, user.Id);

        var act = async () => await service.UpdatePollAsync(
            created.Id, user.Id, new UpdatePollRequest { Title = "New Title" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // -----------------------------------------------------------------------
    // UpdatePollAsync — null fields are NOT applied (partial update)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePollAsync_NullFields_PreservesExistingValues()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);
        var createRequest = new CreatePollRequest
        {
            Title = "Original",
            Description = "Original Desc",
            Options = new List<string> { "A", "B", "C" },
        };
        var created = await service.CreatePollAsync(user.Id, createRequest);

        var result = await service.UpdatePollAsync(
            created.Id, user.Id, new UpdatePollRequest { Title = "New Title" });

        result.Title.Should().Be("New Title");
        result.Description.Should().Be("Original Desc");
        result.Options.Should().HaveCount(3);
    }
}
