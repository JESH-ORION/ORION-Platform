using Dapper;
using Npgsql;
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
            SelectBase + " WHERE id = @Id LIMIT 1;",
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
            SelectBase + " WHERE email = @Email LIMIT 1;",
            new { Email = email.Trim() },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UsuarioReadModel>(command);
    }

    public async Task<UsuarioCreateResult> CriarAsync(
        UsuarioCreateModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.Nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.Email);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        const string perfilExisteSql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM perfil
                WHERE id = @PerfilId
                  AND status = 'ATIVO'
            );
            """;

        var perfilExiste = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                perfilExisteSql,
                new { model.PerfilId },
                cancellationToken: cancellationToken));

        if (!perfilExiste)
        {
            return new UsuarioCreateResult(UsuarioCreateStatus.PerfilNaoEncontrado);
        }

        const string sql = """
            INSERT INTO usuario
            (
                perfil_id,
                nome,
                email,
                telefone,
                senha_hash,
                documento,
                tipo_documento,
                status
            )
            VALUES
            (
                @PerfilId,
                @Nome,
                @Email,
                @Telefone,
                @SenhaHash,
                @Documento,
                @TipoDocumento,
                'ATIVO'
            )
            RETURNING
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
                data_atualizacao AS DataAtualizacao;
            """;

        var parameters = new
        {
            model.PerfilId,
            Nome = model.Nome.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Telefone = string.IsNullOrWhiteSpace(model.Telefone)
                ? null
                : model.Telefone.Trim(),
            SenhaHash = $"PENDING_AUTH::{Guid.NewGuid():N}",
            Documento = string.IsNullOrWhiteSpace(model.Documento)
                ? null
                : model.Documento.Trim(),
            TipoDocumento = string.IsNullOrWhiteSpace(model.TipoDocumento)
                ? null
                : model.TipoDocumento.Trim().ToUpperInvariant()
        };

        try
        {
            var usuario = await connection.QuerySingleAsync<UsuarioReadModel>(
                new CommandDefinition(
                    sql,
                    parameters,
                    cancellationToken: cancellationToken));

            return new UsuarioCreateResult(UsuarioCreateStatus.Criado, usuario);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UniqueViolation
                  && exception.ConstraintName == "uq_usuario_email")
        {
            return new UsuarioCreateResult(UsuarioCreateStatus.EmailDuplicado);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UniqueViolation
                  && exception.ConstraintName == "uq_usuario_documento")
        {
            return new UsuarioCreateResult(UsuarioCreateStatus.DocumentoDuplicado);
        }
    }
}
