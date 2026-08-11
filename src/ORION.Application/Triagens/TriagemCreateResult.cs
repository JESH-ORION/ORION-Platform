namespace ORION.Application.Triagens;

public enum TriagemCreateStatus
{
    Criada = 1,
    PacienteNaoEncontrado = 2,
    ConsultaNaoEncontrada = 3
}

public sealed record TriagemCreateResult(
    TriagemCreateStatus Status,
    TriagemReadModel? Triagem = null);
