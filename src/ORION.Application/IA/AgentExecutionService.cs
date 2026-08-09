using ORION.Application.InteracoesIA;
using ORION.Application.Mensagens;

namespace ORION.Application.IA;

public sealed class AgentExecutionService(
    IInteracaoIARepository interacaoRepository,
    IMensagemRepository mensagemRepository,
    IModeloIAProvider modeloProvider) : IAgentExecutionService
{
    public async Task<AgentExecutionResult> ExecutarAsync(
        AgentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var interacao = await interacaoRepository.ObterPorIdAsync(request.InteracaoId, cancellationToken);
        if (interacao is null)
            return new AgentExecutionResult(AgentExecutionStatus.InteracaoNaoEncontrada);

        if (interacao.Status is not ("INICIADA" or "EM_ANDAMENTO"))
            return new AgentExecutionResult(AgentExecutionStatus.InteracaoInvalida);

        if (!interacao.ConversaId.HasValue)
            return new AgentExecutionResult(AgentExecutionStatus.ConversaIncompativel);

        var mensagem = await mensagemRepository.ObterPorIdAsync(request.MensagemId, cancellationToken);
        if (mensagem is null)
            return new AgentExecutionResult(AgentExecutionStatus.MensagemNaoEncontrada);

        if (mensagem.ConversaId != interacao.ConversaId.Value)
            return new AgentExecutionResult(AgentExecutionStatus.ConversaIncompativel);

        var atualizou = await interacaoRepository.AtualizarStatusAsync(
            interacao.Id,
            "EM_ANDAMENTO",
            cancellationToken: cancellationToken);

        if (!atualizou)
            return new AgentExecutionResult(AgentExecutionStatus.FalhaPersistencia);

        ModeloIAResponse resposta;

        try
        {
            resposta = await modeloProvider.GerarRespostaAsync(
                new ModeloIARequest(
                    interacao.AgenteId,
                    interacao.ConversaId.Value,
                    interacao.PacienteId,
                    mensagem.Conteudo),
                cancellationToken);
        }
        catch
        {
            await interacaoRepository.AtualizarStatusAsync(
                interacao.Id,
                "ERRO",
                modeloProvider.NomeModelo,
                finalizar: true,
                cancellationToken: cancellationToken);

            return new AgentExecutionResult(
                AgentExecutionStatus.FalhaProvider,
                Modelo: modeloProvider.NomeModelo);
        }

        var mensagemResult = await mensagemRepository.CriarAsync(
            new MensagemCreateModel(
                interacao.ConversaId.Value,
                null,
                "AGENTE_IA",
                resposta.Conteudo),
            cancellationToken);

        if (mensagemResult.Status != MensagemCreateStatus.Criada || mensagemResult.Mensagem is null)
        {
            await interacaoRepository.AtualizarStatusAsync(
                interacao.Id,
                "ERRO",
                modeloProvider.NomeModelo,
                finalizar: true,
                cancellationToken: cancellationToken);

            return new AgentExecutionResult(
                AgentExecutionStatus.FalhaPersistencia,
                Modelo: modeloProvider.NomeModelo);
        }

        atualizou = await interacaoRepository.AtualizarStatusAsync(
            interacao.Id,
            "FINALIZADA",
            modeloProvider.NomeModelo,
            finalizar: true,
            cancellationToken: cancellationToken);

        if (!atualizou)
            return new AgentExecutionResult(AgentExecutionStatus.FalhaPersistencia);

        return new AgentExecutionResult(
            AgentExecutionStatus.Sucesso,
            mensagemResult.Mensagem,
            modeloProvider.NomeModelo);
    }
}
