using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Rcv.Web.Api.Infrastructure;

namespace Rcv.Web.Api.Tests.Infrastructure;

public sealed class JwtConfigurationValidationServiceTests
{
    [Theory]
    [InlineData(null, "missing")]
    [InlineData("{REPLACE_IN_PRODUCTION}", "placeholder")]
    [InlineData("1234567890123456789012345678901", "at least 32 bytes")]
    public async Task StartAsync_RejectsUnsafeSigningKey(string? key, string expectedMessage)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SecretKey"] = key,
            })
            .Build();
        var service = new JwtConfigurationValidationService(configuration);

        var act = () => service.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public async Task StartAsync_AcceptsExactly32Bytes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SecretKey"] = new string('a', 32),
            })
            .Build();
        var service = new JwtConfigurationValidationService(configuration);

        var act = () => service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
