using Dapper;
using Npgsql;
using ORION.Application.Pacientes;

namespace ORION.Infrastructure.Persistence.Repositories;

public sealed class PacienteRepository(IDbConnectionFactory connectionFactory) : IPacienteRepository
{
    private const string SelectBase = """
        SELECT
            id AS Id,
            usuario_id AS UsuarioId,
            nome_completo AS NomeCompleto,
            cpf AS Cpf,
            status AS Status
        FROM paciente
        """;

    public async Task<PacienteReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            SelectBase + " WHERE id = @Id LIMIT 1;",
            new { Id = id },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PacienteReadModel>(command);
    }

    public async Task<PacienteCreateResult> CriarAsync(
        PacienteCreateModel model,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        const string usuarioExisteSql = """
            SELECT EXISTS (
                SELECT 1
                FROM usuario
                WHERE id = @UsuarioId
            );
            """;

        var usuarioExiste = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                usuarioExisteSql,
                new { model.UsuarioId },
                cancellationToken: cancellationToken));

        if (!usuarioExiste)
        {
            return new PacienteCreateResult(PacienteCreateStatus.UsuarioNaoEncontrado);
        }

        const string sql = """
            INSERT INTO paciente
            (
                usuario_id,
                nome_completo,
                cpf,
                status
            )
            VALUES
            (
                @UsuarioId,
                @NomeCompleto,
                @Cpf,
                'ATIVO'
            )
            RETURNING
                id AS Id,
                usuario_id AS UsuarioId,
                nome_completo AS NomeCompleto,
                cpf AS Cpf,
                status AS Status;
            """;

        var parameters = new
        {
            model.UsuarioId,
            NomeCompleto = model.NomeCompleto.Trim(),
            Cpf = string.IsNullOrWhiteSpace(model.Cpf)
                ? null
                : model.Cpf.Trim()
        };

        try
        {
            var paciente = await connection.QuerySingleAsync<PacienteReadModel>(
                new CommandDefinition(
                    sql,
                    parameters,
                    cancellationToken: cancellationToken));

            return new PacienteCreateResult(PacienteCreateStatus.Criado, paciente);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UniqueViolation &&
                  exception.ConstraintName == "uq_paciente_usuario")
        {
            return new PacienteCreateResult(PacienteCreateStatus.UsuarioJaPossuiPaciente);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UniqueViolation &&
                  exception.ConstraintName == "uq_paciente_cpf")
        {
            return new PacienteCreateResult(PacienteCreateStatus.CpfDuplicado);
        }
    }
}
