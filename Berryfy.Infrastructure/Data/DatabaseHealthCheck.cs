using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Berryfy.Infrastructure.Data
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(IConfiguration configuration, ILogger<DatabaseHealthCheck> logger)
        {
            _connectionString = PostgresConnectionStrings.Resolve(configuration);
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = new NpgsqlCommand("SELECT 1", connection);
                _ = await command.ExecuteScalarAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed: cannot connect");
                return HealthCheckResult.Unhealthy("Database is not accessible", ex);
            }

            _logger.LogDebug("Database health check passed");
            return HealthCheckResult.Healthy("Database is accessible");
        }
    }
}
