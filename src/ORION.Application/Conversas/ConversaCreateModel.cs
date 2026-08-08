namespace ORION.Application.Conversas;

public sealed class ConversaCreateModel
{
    public Guid? PacienteId { get; set; }
    public Guid? ProfissionalId { get; set; }
    public string Tipo { get; set; } = string.Empty;
}
