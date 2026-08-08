using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rcv.Web.Api.Data;

namespace Rcv.Web.Api.Infrastructure;

/// <summary>
/// Reports readiness by checking database connectivity.
/// </summary>
public sealed class DatabaseReadyHealthCheck : IHealthCheck
{
    private readonly RcvDbContext _context;

    /// <summary>
    /// Initializes the readiness check.
    /// </summary>
    /// <param name="context">The database context to probe.</param>
    public DatabaseReadyHealthCheck(RcvDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await _context.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database is unavailable.");
    }
}
