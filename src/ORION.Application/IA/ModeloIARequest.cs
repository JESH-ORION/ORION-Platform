namespace ORION.Application.IA;

public sealed record ModeloIARequest(
    Guid AgenteId,
    Guid ConversaId,
    Guid? PacienteId,
    string MensagemUsuario);
