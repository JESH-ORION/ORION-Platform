using Dapper;
using ORION.Application.Conversas;

namespace ORION.Infrastructure.Persistence.Repositories;

public sealed class ConversaRepository(IDbConnectionFactory connectionFactory) : IConversaRepository
{
    private const string SelectBase = """
        SELECT
            id AS Id,
            paciente_id AS PacienteId,
            profissional_id AS ProfissionalId,
            tipo AS Tipo,
            status AS Status,
            data_criacao AS DataCriacao,
            data_atualizacao AS DataAtualizacao
        FROM conversa
        """;

    public async Task<ConversaReadModel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ConversaReadModel>(new CommandDefinition(
            SelectBase + " WHERE id = @Id LIMIT 1;",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task<ConversaCreateResult> CriarAsync(ConversaCreateModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        if (model.PacienteId.HasValue)
        {
            var pacienteExiste = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM paciente WHERE id = @Id AND status = 'ATIVO');",
                new { Id = model.PacienteId.Value },
                cancellationToken: cancellationToken));

            if (!pacienteExiste)
                return new ConversaCreateResult(ConversaCreateStatus.PacienteNaoEncontrado);
        }

        if (model.ProfissionalId.HasValue)
        {
            var profissionalExiste = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM profissional WHERE id = @Id AND status = 'ATIVO');",
                new { Id = model.ProfissionalId.Value },
                cancellationToken: cancellationToken));

            if (!profissionalExiste)
                return new ConversaCreateResult(ConversaCreateStatus.ProfissionalNaoEncontrado);
        }

        const string sql = """
            INSERT INTO conversa (paciente_id, profissional_id, tipo, status)
            VALUES (@PacienteId, @ProfissionalId, @Tipo, 'ATIVA')
            RETURNING
                id AS Id,
                paciente_id AS PacienteId,
                profissional_id AS ProfissionalId,
                tipo AS Tipo,
                status AS Status,
                data_criacao AS DataCriacao,
                data_atualizacao AS DataAtualizacao;
            """;

        var conversa = await connection.QuerySingleAsync<ConversaReadModel>(new CommandDefinition(
            sql,
            new
            {
                model.PacienteId,
                model.ProfissionalId,
                Tipo = model.Tipo.Trim().ToUpperInvariant()
            },
            cancellationToken: cancellationToken));

        return new ConversaCreateResult(ConversaCreateStatus.Criada, conversa);
    }
}
