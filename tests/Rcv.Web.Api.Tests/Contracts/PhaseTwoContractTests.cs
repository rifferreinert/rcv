using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Tests.Contracts;

public sealed class PhaseTwoContractTests
{
    [Fact]
    public void PublicAndPersistenceContracts_DoNotExposeIsVotingPublic()
    {
        typeof(Poll).GetProperty("IsVotingPublic").Should().BeNull();
        typeof(CreatePollRequest).GetProperty("IsVotingPublic").Should().BeNull();
        typeof(PollResponse).GetProperty("IsVotingPublic").Should().BeNull();
        typeof(UserSummaryDto).GetProperty("Email").Should().BeNull();
        typeof(UpdatePollRequest).GetProperty("IsResultsPublic").Should().NotBeNull();
    }

    [Fact]
    public void CurrentEfModel_DoesNotContainIsVotingPublic()
    {
        var options = new DbContextOptionsBuilder<RcvDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new RcvDbContext(options);

        context.Model.FindEntityType(typeof(Poll))!
            .FindProperty("IsVotingPublic")
            .Should().BeNull();
    }
}
