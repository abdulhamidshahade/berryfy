using Microsoft.Extensions.Configuration;

namespace Berryfy.Infrastructure.Data
{
    public static class PostgresConnectionStrings
    {
        public static string Resolve(IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("Postgres")
                ?? configuration.GetConnectionString("PostgreSQLServer")
                ?? configuration.GetConnectionString("PostgreConnectionString")
                ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                ?? Environment.GetEnvironmentVariable("DATABASE_URL");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string is not configured. Set ConnectionStrings:Postgres (or PostgreSQLServer / PostgreConnectionString) or POSTGRES_CONNECTION / DATABASE_URL.");
            }

            return connectionString;
        }
    }
}
