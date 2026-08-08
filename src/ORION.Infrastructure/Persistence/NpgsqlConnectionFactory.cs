using System.Data.Common;
using Npgsql;

namespace ORION.Infrastructure.Persistence;

public sealed class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("A connection string do PostgreSQL é obrigatória.", nameof(connectionString))
        : connectionString;

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
