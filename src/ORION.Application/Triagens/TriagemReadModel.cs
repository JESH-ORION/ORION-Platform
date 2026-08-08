namespace ORION.Application.Triagens;

public sealed class TriagemReadModel
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? ConsultaId { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
