using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Requests;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Rcv.Web.Api.Tests.Controllers;

public sealed class SecurityIntegrationTests
{
    [Fact]
    public async Task StateChangingRequest_WithoutAntiforgeryToken_ReturnsProblemDetails()
    {
        await using var factory = new PollsApiFactory();
        var user = CreateUser();
        await factory.SeedUserAsync(user);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Add("Cookie", $"rcv_jwt={factory.CreateJwtForUser(user)}");

        var response = await client.PostAsJsonAsync("/api/polls", new CreatePollRequest
        {
            Title = "CSRF",
            Options = new List<string> { "A", "B" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UnknownResource_ReturnsCentralProblemDetails()
    {
        await using var factory = new PollsApiFactory();
        var response = await factory.CreateClient().GetAsync($"/api/polls/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task PollListing_RequiresAuthenticationAndValidPagination()
    {
        await using var factory = new PollsApiFactory();
        var anonymous = factory.CreateClient();
        (await anonymous.GetAsync("/api/polls")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        var user = CreateUser();
        await factory.SeedUserAsync(user);
        var authenticated = factory.CreateAuthenticatedClient(user);
        (await authenticated.GetAsync("/api/polls?page=0&pageSize=101")).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
        (await authenticated.GetAsync("/api/polls?status=not-a-status")).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthAndDatabaseReadiness_AreHealthy()
    {
        await using var factory = new PollsApiFactory();
        var client = factory.CreateClient();

        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/ready")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Development_DoesNotRedirectHttpToHttps()
    {
        await using var factory = new PollsApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task Production_HostsSpaAndDoesNotFallbackReservedRoutes()
    {
        await using var factory = new ProductionSpaApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        var fallback = await client.GetAsync("/polls/example/results");
        fallback.StatusCode.Should().Be(HttpStatusCode.OK);
        (await fallback.Content.ReadAsStringAsync()).Should().Contain("RCV test SPA");

        var asset = await client.GetStringAsync("/assets/test.txt");
        asset.Should().Contain("static asset");

        var api = await client.GetAsync("/api/not-a-real-endpoint");
        api.StatusCode.Should().Be(HttpStatusCode.NotFound);
        api.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var swagger = await client.GetAsync("/swagger/not-a-real-endpoint");
        swagger.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await swagger.Content.ReadAsStringAsync()).Should().NotContain("RCV test SPA");
    }

    [Fact]
    public async Task Production_RedirectsHttpAndAddsHstsOnHttps()
    {
        await using var factory = new ProductionSpaApiFactory();
        var httpClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });
        var redirect = await httpClient.GetAsync("/health");
        redirect.StatusCode.Should().BeOneOf(
            HttpStatusCode.TemporaryRedirect,
            HttpStatusCode.PermanentRedirect);
        redirect.Headers.Location!.Scheme.Should().Be("https");

        var httpsClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://example.test"),
        });
        var secure = await httpsClient.GetAsync("/health");
        secure.Headers.Should().ContainKey("Strict-Transport-Security");
    }

    [Fact]
    public async Task ResultsEndpoint_WithNoVotes_ReturnsExplicitNoVotesState()
    {
        await using var factory = new PollsApiFactory();
        var user = CreateUser();
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "No votes");

        var response = await factory.CreateClient().GetAsync($"/api/polls/{pollId}/results");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.RootElement.GetProperty("state").GetString().Should().Be("NoVotes");
    }

    public sealed class ProductionSpaApiFactory : PollsApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseWebRoot(Path.Combine(
                AppContext.BaseDirectory,
                "TestAssets",
                "wwwroot"));
            builder.ConfigureServices(services =>
                services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = 443));
            base.ConfigureWebHost(builder);
        }
    }

    [Fact]
    public async Task VoteRateLimit_EventuallyReturnsTooManyRequests()
    {
        await using var factory = new VotesApiFactory();
        var user = CreateUser();
        await factory.SeedUserAsync(user);
        var (pollId, optionIds) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateAuthenticatedClient(user);
        HttpResponseMessage? response = null;

        for (var request = 0; request < 31; request++)
        {
            response = await client.PostAsJsonAsync(
                $"/api/polls/{pollId}/votes",
                new CastVoteRequest { RankedOptionIds = optionIds });
        }

        response!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task AuthRateLimit_EventuallyReturnsTooManyRequests()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();
        HttpResponseMessage? response = null;

        for (var request = 0; request < 21; request++)
            response = await client.GetAsync("/api/auth/login/unknown");

        response!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void Startup_RejectsCheckedInJwtPlaceholder()
    {
        using var factory = new InvalidJwtApiFactory();

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretKey*placeholder*");
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        ExternalId = Guid.NewGuid().ToString(),
        Provider = "Google",
        CreatedAt = DateTime.UtcNow,
    };

    private sealed class InvalidJwtApiFactory : AuthApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Jwt:SecretKey"] = "{REPLACE_IN_PRODUCTION}",
                }));
        }
    }
}
