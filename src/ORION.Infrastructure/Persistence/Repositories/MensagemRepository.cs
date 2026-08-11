using Dapper;
using ORION.Application.Mensagens;

namespace ORION.Infrastructure.Persistence.Repositories;

public sealed class MensagemRepository(IDbConnectionFactory connectionFactory) : IMensagemRepository
{
    private const string SelectBase = """
        SELECT
            id AS Id,
            conversa_id AS ConversaId,
            usuario_id AS UsuarioId,
            tipo AS Tipo,
            conteudo AS Conteudo,
            data_envio AS DataEnvio,
            lida_em AS LidaEm
        FROM mensagem
        """;

    public async Task<MensagemReadModel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            SelectBase + " WHERE id = @Id LIMIT 1;",
            new { Id = id },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<MensagemReadModel>(command);
    }

    public async Task<MensagemCreateResult> CriarAsync(MensagemCreateModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var conversaExiste = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM conversa WHERE id = @ConversaId AND status = 'ATIVA');",
                new { model.ConversaId },
                cancellationToken: cancellationToken));

        if (!conversaExiste)
            return new MensagemCreateResult(MensagemCreateStatus.ConversaNaoEncontrada);

        if (model.UsuarioId.HasValue)
        {
            var usuarioExiste = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    "SELECT EXISTS(SELECT 1 FROM usuario WHERE id = @UsuarioId AND status = 'ATIVO');",
                    new { model.UsuarioId },
                    cancellationToken: cancellationToken));

            if (!usuarioExiste)
                return new MensagemCreateResult(MensagemCreateStatus.UsuarioNaoEncontrado);
        }

        const string sql = """
            INSERT INTO mensagem (conversa_id, usuario_id, tipo, conteudo)
            VALUES (@ConversaId, @UsuarioId, @Tipo, @Conteudo)
            RETURNING
                id AS Id,
                conversa_id AS ConversaId,
                usuario_id AS UsuarioId,
                tipo AS Tipo,
                conteudo AS Conteudo,
                data_envio AS DataEnvio,
                lida_em AS LidaEm;
            """;

        var mensagem = await connection.QuerySingleAsync<MensagemReadModel>(
            new CommandDefinition(
                sql,
                new
                {
                    model.ConversaId,
                    model.UsuarioId,
                    Tipo = model.Tipo.Trim(),
                    Conteudo = model.Conteudo.Trim()
                },
                cancellationToken: cancellationToken));

        return new MensagemCreateResult(MensagemCreateStatus.Criada, mensagem);
    }
}
