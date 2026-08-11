using Dapper;
using Npgsql;
using ORION.Application.Agentes;

namespace ORION.Infrastructure.Persistence.Repositories;

public sealed class AgenteRepository(IDbConnectionFactory connectionFactory) : IAgenteRepository
{
    private const string SelectBase = """
        SELECT
            id AS Id,
            identificador AS Identificador,
            nome AS Nome,
            versao AS Versao,
            finalidade AS Finalidade,
            status AS Status,
            data_criacao AS DataCriacao,
            data_atualizacao AS DataAtualizacao
        FROM agente
        """;

    public async Task<AgenteReadModel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AgenteReadModel>(new CommandDefinition(
            SelectBase + " WHERE id = @Id LIMIT 1;",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task<AgenteCreateResult> CriarAsync(AgenteCreateModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO agente (identificador, nome, versao, finalidade, status)
            VALUES (@Identificador, @Nome, @Versao, @Finalidade, 'ATIVO')
            RETURNING
                id AS Id,
                identificador AS Identificador,
                nome AS Nome,
                versao AS Versao,
                finalidade AS Finalidade,
                status AS Status,
                data_criacao AS DataCriacao,
                data_atualizacao AS DataAtualizacao;
            """;

        try
        {
            var agente = await connection.QuerySingleAsync<AgenteReadModel>(new CommandDefinition(
                sql,
                new
                {
                    Identificador = model.Identificador.Trim(),
                    Nome = model.Nome.Trim(),
                    Versao = model.Versao.Trim(),
                    Finalidade = string.IsNullOrWhiteSpace(model.Finalidade) ? null : model.Finalidade.Trim()
                },
                cancellationToken: cancellationToken));

            return new AgenteCreateResult(AgenteCreateStatus.Criado, agente);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.UniqueViolation &&
            ex.ConstraintName == "uq_agente_identificador_versao")
        {
            return new AgenteCreateResult(AgenteCreateStatus.IdentificadorVersaoDuplicado);
        }
    }
}
