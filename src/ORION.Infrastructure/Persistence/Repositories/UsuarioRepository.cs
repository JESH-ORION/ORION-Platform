using Dapper;
using ORION.Application.Usuarios;

namespace ORION.Infrastructure.Persistence.Repositories;

public sealed class UsuarioRepository(IDbConnectionFactory connectionFactory) : IUsuarioRepository
{
    private const string SelectBase = """
        SELECT
            id AS Id,
            perfil_id AS PerfilId,
            nome AS Nome,
            email AS Email,
            telefone AS Telefone,
            documento AS Documento,
            tipo_documento AS TipoDocumento,
            status AS Status,
            ultimo_acesso AS UltimoAcesso,
            data_criacao AS DataCriacao,
            data_atualizacao AS DataAtualizacao
        FROM usuario
        """;

    public async Task<UsuarioReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            SelectBase + "WHERE id = @Id LIMIT 1;",
            new { Id = id },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UsuarioReadModel>(command);
    }

    public async Task<UsuarioReadModel?> ObterPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            SelectBase + "WHERE email = @Email LIMIT 1;",
            new { Email = email.Trim() },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UsuarioReadModel>(command);
    }
}
