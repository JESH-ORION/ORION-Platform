namespace ORION.Application.Triagens;

public sealed class TriagemCreateModel
{
    public Guid PacienteId { get; set; }
    public Guid? ConsultaId { get; set; }
    public string Origem { get; set; } = string.Empty;
}
