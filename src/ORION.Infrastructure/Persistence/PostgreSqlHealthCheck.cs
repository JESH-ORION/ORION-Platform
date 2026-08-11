using Dapper;

namespace ORION.Infrastructure.Persistence;

public sealed class PostgreSqlHealthCheck(IDbConnectionFactory connectionFactory)
{
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var result = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                "SELECT 1;",
                cancellationToken: cancellationToken));

        return result == 1;
    }
}
