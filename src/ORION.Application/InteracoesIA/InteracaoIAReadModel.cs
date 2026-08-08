namespace ORION.Application.InteracoesIA;

public sealed class InteracaoIAReadModel
{
    public Guid Id { get; set; }
    public Guid AgenteId { get; set; }
    public Guid? ConversaId { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? Modelo { get; set; }
    public string? VersaoAgente { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}
