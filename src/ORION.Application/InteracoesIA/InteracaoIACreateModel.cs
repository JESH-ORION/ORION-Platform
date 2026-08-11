namespace ORION.Application.InteracoesIA;

public sealed record InteracaoIACreateModel(
    Guid AgenteId,
    Guid? ConversaId,
    Guid? PacienteId,
    Guid? UsuarioId,
    string? Modelo);
