namespace Rcv.Web.Api.Infrastructure;

/// <summary>
/// Rejects unsafe JWT signing configuration when the application starts.
/// </summary>
public sealed class JwtConfigurationValidationService : IHostedService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes startup JWT validation.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public JwtConfigurationValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        JwtSigningKeyValidator.CreateSecurityKey(
            _configuration["Authentication:Jwt:SecretKey"]);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
