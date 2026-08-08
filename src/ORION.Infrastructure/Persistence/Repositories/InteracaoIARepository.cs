using Dapper;
using ORION.Application.InteracoesIA;

namespace ORION.Infrastructure.Persistence.Repositories;

public sealed class InteracaoIARepository(IDbConnectionFactory connectionFactory) : IInteracaoIARepository
{
    private const string SelectBase = """
        SELECT
            id AS Id,
            agente_id AS AgenteId,
            conversa_id AS ConversaId,
            paciente_id AS PacienteId,
            usuario_id AS UsuarioId,
            modelo AS Modelo,
            versao_agente AS VersaoAgente,
            status AS Status,
            data_inicio AS DataInicio,
            data_fim AS DataFim
        FROM interacao_ia
        """;

    public async Task<InteracaoIAReadModel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            SelectBase + " WHERE id = @Id LIMIT 1;",
            new { Id = id },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<InteracaoIAReadModel>(command);
    }

    public async Task<InteracaoIACreateResult> CriarAsync(InteracaoIACreateModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var agente = await connection.QuerySingleOrDefaultAsync<(Guid Id, string Versao)>(
            new CommandDefinition(
                "SELECT id AS Id, versao AS Versao FROM agente WHERE id = @AgenteId AND status = 'ATIVO' LIMIT 1;",
                new { model.AgenteId },
                cancellationToken: cancellationToken));

        if (agente.Id == Guid.Empty)
            return new InteracaoIACreateResult(InteracaoIACreateStatus.AgenteNaoEncontrado);

        if (model.ConversaId.HasValue)
        {
            var existe = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM conversa WHERE id = @ConversaId AND status = 'ATIVA');",
                new { model.ConversaId }, cancellationToken: cancellationToken));
            if (!existe) return new InteracaoIACreateResult(InteracaoIACreateStatus.ConversaNaoEncontrada);
        }

        if (model.PacienteId.HasValue)
        {
            var existe = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM paciente WHERE id = @PacienteId AND status = 'ATIVO');",
                new { model.PacienteId }, cancellationToken: cancellationToken));
            if (!existe) return new InteracaoIACreateResult(InteracaoIACreateStatus.PacienteNaoEncontrado);
        }

        if (model.UsuarioId.HasValue)
        {
            var existe = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM usuario WHERE id = @UsuarioId AND status = 'ATIVO');",
                new { model.UsuarioId }, cancellationToken: cancellationToken));
            if (!existe) return new InteracaoIACreateResult(InteracaoIACreateStatus.UsuarioNaoEncontrado);
        }

        const string sql = """
            INSERT INTO interacao_ia
                (agente_id, conversa_id, paciente_id, usuario_id, modelo, versao_agente, status, data_inicio)
            VALUES
                (@AgenteId, @ConversaId, @PacienteId, @UsuarioId, @Modelo, @VersaoAgente, 'INICIADA', now())
            RETURNING
                id AS Id,
                agente_id AS AgenteId,
                conversa_id AS ConversaId,
                paciente_id AS PacienteId,
                usuario_id AS UsuarioId,
                modelo AS Modelo,
                versao_agente AS VersaoAgente,
                status AS Status,
                data_inicio AS DataInicio,
                data_fim AS DataFim;
            """;

        var interacao = await connection.QuerySingleAsync<InteracaoIAReadModel>(new CommandDefinition(
            sql,
            new
            {
                model.AgenteId,
                model.ConversaId,
                model.PacienteId,
                model.UsuarioId,
                Modelo = string.IsNullOrWhiteSpace(model.Modelo) ? null : model.Modelo.Trim(),
                VersaoAgente = agente.Versao
            },
            cancellationToken: cancellationToken));

        return new InteracaoIACreateResult(InteracaoIACreateStatus.Criada, interacao);
    }
}
