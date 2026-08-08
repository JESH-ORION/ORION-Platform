namespace ORION.Application.Agentes;

public enum AgenteCreateStatus
{
    Criado = 1,
    IdentificadorVersaoDuplicado = 2
}

public sealed record AgenteCreateResult(
    AgenteCreateStatus Status,
    AgenteReadModel? Agente = null);
