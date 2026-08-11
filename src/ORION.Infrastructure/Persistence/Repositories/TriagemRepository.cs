using Dapper;
using ORION.Application.Triagens;

namespace ORION.Infrastructure.Persistence.Repositories;

public sealed class TriagemRepository(IDbConnectionFactory connectionFactory) : ITriagemRepository
{
    private const string SelectBase = """
        SELECT
            id AS Id,
            paciente_id AS PacienteId,
            consulta_id AS ConsultaId,
            origem AS Origem,
            status AS Status
        FROM triagem
        """;

    public async Task<TriagemReadModel?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            SelectBase + " WHERE id = @Id LIMIT 1;",
            new { Id = id },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<TriagemReadModel>(command);
    }

    public async Task<TriagemCreateResult> CriarAsync(
        TriagemCreateModel model,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        const string pacienteExisteSql = """
            SELECT EXISTS(
                SELECT 1
                FROM paciente
                WHERE id = @PacienteId
                  AND status = 'ATIVO'
            );
            """;

        var pacienteExiste = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                pacienteExisteSql,
                new { model.PacienteId },
                cancellationToken: cancellationToken));

        if (!pacienteExiste)
        {
            return new TriagemCreateResult(TriagemCreateStatus.PacienteNaoEncontrado);
        }

        if (model.ConsultaId.HasValue)
        {
            const string consultaExisteSql = """
                SELECT EXISTS(
                    SELECT 1
                    FROM consulta
                    WHERE id = @ConsultaId
                );
                """;

            var consultaExiste = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    consultaExisteSql,
                    new { model.ConsultaId },
                    cancellationToken: cancellationToken));

            if (!consultaExiste)
            {
                return new TriagemCreateResult(TriagemCreateStatus.ConsultaNaoEncontrada);
            }
        }

        const string sql = """
            INSERT INTO triagem
            (
                paciente_id,
                consulta_id,
                origem,
                status
            )
            VALUES
            (
                @PacienteId,
                @ConsultaId,
                @Origem,
                'INICIADA'
            )
            RETURNING
                id AS Id,
                paciente_id AS PacienteId,
                consulta_id AS ConsultaId,
                origem AS Origem,
                status AS Status;
            """;

        var triagem = await connection.QuerySingleAsync<TriagemReadModel>(
            new CommandDefinition(
                sql,
                new
                {
                    model.PacienteId,
                    model.ConsultaId,
                    Origem = model.Origem.Trim().ToUpperInvariant()
                },
                cancellationToken: cancellationToken));

        return new TriagemCreateResult(TriagemCreateStatus.Criada, triagem);
    }
}
